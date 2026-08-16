# lightr

 [![.NET](https://img.shields.io/nuget/v/Lightr?link=https%3A%2F%2Fwww.nuget.org%2Fpackages%2FLightr)](https://www.nuget.org/packages/Lightr)
 [![.NET](https://github.com/sommmen/lightr/actions/workflows/build-ci.yml/badge.svg)](https://github.com/sommmen/lightr/actions/workflows/build-ci.yml)

C# api for https://lightr.nl/ a SAAS to send handwritten cards via an API.

# Getting started

Install the package:
``` powershell
dotnet add package Lightr
```

Then simply add the `ILightrClient` to your DI using:

``` csharp
services.AddLightr("my-token");
```

Or:

``` csharp
services
    .AddLightr((provider, options) =>
    {
        var token = provider.GetRequiredService<ILightrSettings>().ApiKey;
        options.UseToken(token);
    });
```

Then use this service;

``` csharp
public class MyAwesomeApp
{
    private readonly ILightrClient _lightrClient;

    public MyAwesomeApp(ILightrClient lightrClient)
    {
        _lightrClient = lightrClient;
    }

    public async Task MyBusinessMethod(CancellationToken cancellationToken = default)
    {
        await _lightrClient.MeAsync(cancellationToken);
    }
}
```

For more information check the [sample](https://github.com/sommmen/lightr/tree/main/sample).

# Rate Limits

A `LightrRateLimitedHandler` is added by default which handles the rate limits of the API. 
Requests are delayed when the limit is reached. See the `ServiceCollectionExtensions` for the precise configuration.

# Development

The library is generated from the upstream OpenAPI document and requires the .NET 10 SDK. Restore the repository-local tools before building:

```powershell
dotnet tool restore
dotnet build
```

To refresh the checked-in OpenAPI document, run:

```powershell
.\scripts\Refresh-OpenApi.ps1
dotnet build
```

The build automatically applies `schema-corrections.overlay.yaml` before NSwag generates the client. The overlay corrects known upstream schema defects, so no manual post-processing of the downloaded document is needed.
