using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Lightr;

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
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddLightr(this IServiceCollection services, string token, Action<HttpClient>? configureClient = null)
    {
        services.AddHttpClient<ILightrClient, LightrClient>("lightr", c =>
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            configureClient?.Invoke(c);
        });
        
        return services;
    }
}