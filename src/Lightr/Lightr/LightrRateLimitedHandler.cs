using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace Lightr;

/// <summary>
/// Delays requests to stay within the API budget and honours <c>Retry-After</c> on a server 429.
/// See: <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/http-ratelimiter"/>
/// </summary>
/// <remarks>The limiter is shared across handler instances and owned by <see cref="LightrRateLimiter"/>, so this handler never disposes it.</remarks>
internal sealed class LightrRateLimitedHandler(LightrRateLimiter limiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await SendLimitedAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.TooManyRequests
            || !limiter.TryGetRetryDelay(response, out var delay))
        {
            return response;
        }

        response.Dispose();
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

        return await SendLimitedAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendLimitedAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var lease = await limiter.Limiter.AcquireAsync(permitCount: 1, cancellationToken).ConfigureAwait(false);

        if (lease.IsAcquired)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Local queue is full: surface a 429 without Retry-After so it is not retried here.
        return new HttpResponseMessage(HttpStatusCode.TooManyRequests) { RequestMessage = request };
    }
}
