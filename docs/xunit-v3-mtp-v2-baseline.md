# Test stack baseline: xUnit v3 on Microsoft Testing Platform v2

Status: **compliant**, with one version ahead of the estate target (see the table).
This repository already matches the estate-wide standard, so this document records
the verified baseline rather than a migration.

The shared standard, target recipe and CI command reference live in the `stallions`
repository at `docs/guides/Testing/xunit-v3-mtp-v2-standard.md`.

## 1. Current state

Single test project: `sample/SampleLightrApp.Tests/SampleLightrApp.Tests.csproj`,
targeting `net10.0` only. Package versions are managed centrally in
`Directory.Packages.props`, so the test project carries no version numbers.

| Concern | Current value | Target | Verdict |
|---|---|---|---|
| Framework + runner | `xunit.v3.mtp-v2` **4.0.2** (`Directory.Packages.props`) | `xunit.v3.mtp-v2` 4.0.1 | ✅ (newer patch) |
| `OutputType` | `Exe` | `Exe` | ✅ |
| `TestingPlatformDotnetTestSupport` | `true` | `true` | ✅ |
| VSTest remnants | none | none | ✅ |
| CI reporting | `Microsoft.Testing.Extensions.GitHubActionsReport` **2.5.2** (`Directory.Packages.props`) | 2.4.1 (estate doc, not yet updated) | ⚠️ ahead of target |
| HTTP stubbing | `WireMock.Net` 2.21.0 (`Directory.Packages.props`) | repository choice | ✅ |
| Runner selection | `global.json` `test.runner` = `Microsoft.Testing.Platform` | `test.runner` block | ✅ |
| Warnings | `TreatWarningsAsErrors` = `true` (root `Directory.Build.props`) | repository choice | ✅ |

CI already uses the MTP reporting flag: both `.github/workflows/build-ci.yml` and
`.github/workflows/deploy.yml` run `dotnet test ... --report-gh`.

## 2. Remaining work

None. The two items this section tracked are done:

- The reporting extension is pinned to 2.5.2 in `Directory.Packages.props`, the
  only place it is versioned.
- `global.json` at the repository root declares the SDK (`10.0.401`, `latestFeature`
  roll-forward) and the test runner (`Microsoft.Testing.Platform`). CI installs
  `10.0.x`, which satisfies that pin.

The SDK pin does not change target frameworks: the library,
`src/Lightr/Lightr/Lightr.csproj`, targets `netstandard2.1` only, and the test
project targets `net10.0`.

## 3. Verification

```powershell
dotnet restore lightr.sln
dotnet build lightr.sln --configuration Release
dotnet test --no-build --configuration Release --report-gh
```

Record the test count before and after; it must not change. Both steps in this
document are packaging-only and must not alter the number of tests discovered.
