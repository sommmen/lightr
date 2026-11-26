using System.Diagnostics;
using Lightr;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using Response = WireMock.ResponseBuilders.Response;
using WireMock.Server;

namespace SampleLightrApp.Tests;

public sealed class LightrRateLimitTests : IDisposable
{
    private const string FontsPath = "/api/v1/fonts";
    private const string EmptyFonts = """{"data":[]}""";

    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Dispose();

    [Fact]
    public async Task The_budget_survives_handler_rotation()
    {
        _server
            .Given(Request.Create().WithPath(FontsPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(EmptyFonts));

        await using var provider = BuildProvider(
            new LightrRateLimitOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 },
            TimeSpan.FromSeconds(1));
        var cancellationToken = TestContext.Current.CancellationToken;

        for (var i = 0; i < 3; i++)
        {
            await CreateClient(provider).FontsAsync(cancellationToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(2.5), cancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(() => CreateClient(provider).FontsAsync(cancellationToken));

        Assert.Equal(429, exception.StatusCode);
        Assert.Equal(3, _server.LogEntries.Count());
    }

    [Fact]
    public async Task A_server_429_with_Retry_After_is_retried_once()
    {
        _server
            .Given(Request.Create().WithPath(FontsPath).UsingGet())
            .InScenario("rate-limit")
            .AtPriority(2)
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "1"));
        _server
            .Given(Request.Create().WithPath(FontsPath).UsingGet())
            .InScenario("rate-limit")
            .WhenStateIs("recovered")
            .AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBody(EmptyFonts));

        await using var provider = BuildProvider(new LightrRateLimitOptions(), handlerLifetime: null);
        var stopwatch = Stopwatch.StartNew();

        var fonts = await CreateClient(provider).FontsAsync(TestContext.Current.CancellationToken);

        Assert.Empty(fonts.Data);
        Assert.Equal(2, _server.LogEntries.Count());
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromSeconds(0.9), $"Retried after only {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task A_server_429_is_surfaced_when_Retry_After_exceeds_the_maximum_wait()
    {
        _server
            .Given(Request.Create().WithPath(FontsPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "120"));

        await using var provider = BuildProvider(new LightrRateLimitOptions(), handlerLifetime: null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => CreateClient(provider).FontsAsync(TestContext.Current.CancellationToken));

        Assert.Equal(429, exception.StatusCode);
        Assert.Single(_server.LogEntries);
    }

    private ServiceProvider BuildProvider(LightrRateLimitOptions rateLimitOptions, TimeSpan? handlerLifetime)
    {
        var services = new ServiceCollection();
        services.Configure<LightrRateLimitOptions>(o =>
        {
            o.PermitLimit = rateLimitOptions.PermitLimit;
            o.Window = rateLimitOptions.Window;
            o.QueueLimit = rateLimitOptions.QueueLimit;
            o.MaxRetryAfter = rateLimitOptions.MaxRetryAfter;
        });

        var builder = services.AddLightr("token");
        if (handlerLifetime is { } lifetime)
        {
            builder.SetHandlerLifetime(lifetime);
        }

        return services.BuildServiceProvider();
    }

    private LightrClient CreateClient(ServiceProvider provider)
    {
        var client = (LightrClient)provider.GetRequiredService<ILightrClient>();
        client.BaseUrl = _server.Url!;
        return client;
    }
}