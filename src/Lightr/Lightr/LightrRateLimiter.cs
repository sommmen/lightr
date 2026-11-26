using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Lightr;

/// <summary>
/// Singleton owner of the one limiter shared by every handler chain that <c>IHttpClientFactory</c> builds,
/// so a handler rotation does not hand out a fresh budget.
/// </summary>
internal sealed class LightrRateLimiter : IDisposable
{
    private const int SegmentsPerWindow = 12;

    private readonly LightrRateLimitOptions _options;

    public LightrRateLimiter(IOptions<LightrRateLimitOptions> options)
    {
        _options = options.Value;
        Limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = _options.PermitLimit,
            Window = _options.Window,
            SegmentsPerWindow = SegmentsPerWindow,
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = _options.QueueLimit
        });
    }

    public RateLimiter Limiter { get; }

    public bool TryGetRetryDelay(HttpResponseMessage response, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;

        if (_options.MaxRetryAfter <= TimeSpan.Zero || GetServerDelay(response) is not { } serverDelay)
        {
            return false;
        }

        if (serverDelay > _options.MaxRetryAfter)
        {
            return false;
        }

        delay = serverDelay > TimeSpan.Zero ? serverDelay : TimeSpan.Zero;
        return true;
    }

    private static TimeSpan? GetServerDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            return date - DateTimeOffset.UtcNow;
        }

        // The server's X-RateLimit-Reset format is unrecorded; accept both a unix timestamp and seconds-until-reset.
        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
            && long.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var reset))
        {
            return reset > 1_000_000_000
                ? DateTimeOffset.FromUnixTimeSeconds(reset) - DateTimeOffset.UtcNow
                : TimeSpan.FromSeconds(reset);
        }

        return null;
    }
    public void Dispose() => Limiter.Dispose();
}
