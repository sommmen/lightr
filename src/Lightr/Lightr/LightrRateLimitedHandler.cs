using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

namespace Lightr;

/// <summary>
/// See: <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/http-ratelimiter"/>
/// </summary>
/// <param name="limiter"></param>
internal sealed class LightrRateLimitedHandler(RateLimiter limiter) : DelegatingHandler, IAsyncDisposable
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        while (true)
        {
            using RateLimitLease lease = await limiter.AcquireAsync(
                permitCount: 1, cancellationToken);

            if (lease.IsAcquired)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            if (lease.TryGetMetadata(
                    MetadataName.RetryAfter, out var retryAfter))
            {
                response.Headers.Add(
                    "Retry-After",
                    ((int)retryAfter.TotalSeconds).ToString(
                        NumberFormatInfo.InvariantInfo));
            }

            return response;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await limiter.DisposeAsync().ConfigureAwait(false);

        Dispose(disposing: false);
        // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor Reason: MS recommends this
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            limiter.Dispose();
        }
    }
}