using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Lightr;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace In MS namespace to help with discoverability
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ILightrClient"/> named HTTP client.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="token">Bearer token, see: <see href="https://app.lightr.nl/dashboard/api-tokens"/></param>
    /// <param name="configureClient">A delegate that is used to configure a <see cref="HttpClient"/>.</param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for any further http client configuration needs.</returns>
    public static IHttpClientBuilder AddLightr(this IServiceCollection services, string token, Action<IServiceProvider, HttpClient>? configureClient = null)
    {
        return services.AddHttpClient<ILightrClient, LightrClient>("lightr", (s, c) =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            configureClient?.Invoke(s, c);
        });
    }

    /// <summary>
    /// Registers the <see cref="ILightrClient"/> named HTTP client.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureLightr">A delegate used to configure the <see cref="ILightrClient"/></param>
    /// <returns>The <see cref="IHttpClientBuilder"/> for any further http client configuration needs.</returns>
    public static IHttpClientBuilder AddLightr(this IServiceCollection services, Action<IServiceProvider, LightrOptions> configureLightr)
    {
        return services.AddHttpClient<ILightrClient, LightrClient>("lightr", (s, c) =>
        {
            var options = new LightrOptions
            {
                HttpClient = c
            };

            configureLightr(s, options);

            // We don't validate that we have a valid token, because that way the DI construction would crash.
            // Instead, we just crash on the first API call because we have no token (not very fail-fast, but more user-friendly).
            if (string.IsNullOrWhiteSpace(options.Token))
            {
                s.GetService<ILogger<LightrClient>>()?.LogWarning("Lightr has an empty or missing token (Api Key)");
            }

            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
        });
    }
}

public class LightrOptions
{
    internal string? Token { get; set; }
    public HttpClient HttpClient { get; internal set;  } = null!;

    // TODO 05012025 make this chainable, look at how other libraries do this.

    /// <summary>
    /// Adds a Bearer token, see: <see href="https://app.lightr.nl/dashboard/api-tokens"/>
    /// </summary>
    /// <param name="token"></param>
    public void UseToken(string token)
    {
        Token = token;
    }
}

internal class Test
{
    void t1()
    {
        var s = new ServiceCollection();

        s.AddLightr((s, o) => o.UseToken(""));
    }
}