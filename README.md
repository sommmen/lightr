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

For more information check the sample.

# Development

The project should build as-is.

The library is basically a wrapper to an api document.
It uses the .net open api integration.

To update the open api document follow the steps below.
 - Within visual studio open the connected services tab for the project.
 - Open the dropdown menu for the openapi document, and press refresh.
 - Rebuild the project.