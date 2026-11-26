using System;

namespace Lightr;

/// <summary>
/// Client-side rate limit settings. Configure with <c>services.Configure&lt;LightrRateLimitOptions&gt;(...)</c>.
/// </summary>
public sealed class LightrRateLimitOptions
{
    /// <summary>
    /// Requests allowed in any <see cref="Window"/>. The server allows 120 per minute; the default leaves headroom
    /// for latency and for a server window that is not aligned with ours.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Length of the sliding window.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Requests that may wait for a permit. Beyond this, the request fails with a local 429.
    /// </summary>
    public int QueueLimit { get; set; } = 120;

    /// <summary>
    /// Longest <c>Retry-After</c> the client waits for before retrying a server 429 once.
    /// A longer wait, a missing header or <see cref="TimeSpan.Zero"/> returns the 429 to the caller.
    /// </summary>
    public TimeSpan MaxRetryAfter { get; set; } = TimeSpan.FromSeconds(60);
}
