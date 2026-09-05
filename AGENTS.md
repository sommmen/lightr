# Repository guidance

## Overview

lightr is a C# API client for [Lightr](https://lightr.nl/), a SAAS that sends
handwritten cards via an API. The client library is generated from the
upstream OpenAPI document and targets the .NET 10 SDK. The repository also
contains a sample app under [sample/](sample) demonstrating usage.

## Commit conventions

Use Conventional Commits for every commit message and pull request title:

```
<type>(<scope>): <description>
```

- `type` — one of `feat`, `fix`, `chore`, `docs`, `refactor`, `test`, `build`,
  `ci`, `perf`, `style`
- `scope` — the module, package, or area the change touches
- `description` — a short, imperative summary of the change

Examples:

- `feat(auth): add refresh token rotation`
- `fix(api): handle null response from upstream service`
- `chore(deps): bump dependency versions`
