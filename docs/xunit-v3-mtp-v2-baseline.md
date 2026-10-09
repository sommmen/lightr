# Test stack baseline: xUnit v3 on Microsoft Testing Platform v2

Status: **compliant**. This repository already matches the estate-wide standard, so
this document records the verified baseline rather than a migration.

The shared standard, target recipe and CI command reference live in the `stallions`
repository at `docs/guides/Testing/xunit-v3-mtp-v2-standard.md`.

## 1. Current state

Single test project: `sample/SampleLightrApp.Tests/SampleLightrApp.Tests.csproj`.

| Concern | Current value | Target | Verdict |
|---|---|---|---|
| Framework + runner | `xunit.v3.mtp-v2` **4.0.1** | `xunit.v3.mtp-v2` 4.0.1 | ✅ |
| `OutputType` | `Exe` | `Exe` | ✅ |
| `TestingPlatformDotnetTestSupport` | `true` | `true` | ✅ |
| VSTest remnants | none | none | ✅ |
| CI reporting | `Microsoft.Testing.Extensions.GitHubActionsReport` **2.4.0** | 2.4.1 | ⚠️ patch drift |
| HTTP stubbing | `WireMock.Net` 2.15.0 | repository choice | ✅ |
| Runner selection | no `global.json` | `test.runner` block | ⚠️ implicit |

CI already uses the MTP reporting flag: both `.github/workflows/build-ci.yml` and
`.github/workflows/deploy.yml` run `dotnet test ... --report-gh`.

## 2. Remaining work

Two small items, neither of which changes behaviour today.

### 2.1 Align `Microsoft.Testing.Extensions.GitHubActionsReport` to 2.4.1

`sample/SampleLightrApp.Tests/SampleLightrApp.Tests.csproj` pins 2.4.0. The rest of
the estate (`opg-platform`, `opg-systems`) pins 2.4.1. Move to 2.4.1 so one version
is in play everywhere.

Note on the newer 2.5.1: it requires `Microsoft.Testing.Platform >= 2.5.1`, whereas
`xunit.v3.core.mtp-v2` 4.0.1 brings `Microsoft.Testing.Platform 2.4.0`. NuGet will
resolve upward and this generally works, but it means the MTP version is driven by the
extension rather than by xUnit. Stay on 2.4.1 until the whole estate moves together.

### 2.2 Add a `global.json` runner block

The project works today because `TestingPlatformDotnetTestSupport=true` is set in the
project file, but the repository does not state the runner choice at the repository
level. Add `global.json` next to `lightr.sln`:

```json
{
  "sdk": {
    "version": "10.0.401",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

The `sdk` block is the second reason to add the file: this repository currently pins
the SDK only in CI (`dotnet-version: 10.0.x`), so local builds use whatever is
installed. `10.0.401` matches the pin already used by `opg-platform` and `opg-systems`.

Caveat to check when doing this: `src/Lightr/Lightr/Lightr.csproj` multi-targets and
includes `netstandard2.1`. A `global.json` SDK pin does not change target frameworks,
so this is safe, but confirm the library still restores for both targets after adding
the file.

## 3. Verification

```powershell
dotnet restore lightr.sln
dotnet build lightr.sln --configuration Release
dotnet test --no-build --configuration Release --report-gh
```

Record the test count before and after; it must not change. Both steps in this
document are packaging-only and must not alter the number of tests discovered.
