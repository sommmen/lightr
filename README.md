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

Then use this service;

```
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

