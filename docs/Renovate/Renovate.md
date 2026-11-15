# Renovate

Dependency updates are opened and automerged by a self-hosted Renovate run,
defined in [.github/workflows/renovate.yml](../../.github/workflows/renovate.yml).
The configuration is split across two files:

- [renovate-config.json](../../renovate-config.json) is the runner config passed
  to the action via `configurationFile:`. It names the platform and the
  repository (`sommmen/lightr`).
- [renovate.json](../../renovate.json) holds the rules: the presets
  (`config:recommended`, `:semanticCommits`), the release-age gate, and the
  `packageRules` that decide what automerges.

## Secrets

- `RENOVATE_TOKEN`: a personal access token stored as a repository secret. The
  workflow passes it to the action as the Renovate token.

The workflow uses a PAT rather than the default `GITHUB_TOKEN` because, as the
workflow comment notes, a PAT lets automerge trigger further workflow runs, such
as the required `build` check.

The workflow's own `GITHUB_TOKEN` is limited to `contents: read` and
`security-events: read`. Nothing in the workflow needs more.

### HostRules

None are configured. nuget.org is the only package source
([nuget.config](../../nuget.config)), and Renovate reads it without credentials.
Add a `hostRules` entry only if a package starts coming from a private or
organisation feed.

### Validating the config

Run this from the repository root after editing either config file:

```powershell
npx --yes --package renovate -- renovate-config-validator renovate.json renovate-config.json
```

## Workflow triggers

The workflow runs on three triggers:

- **Schedule:** daily at 08:00 UTC (`cron: "0 8 * * *"`).
- **`workflow_dispatch`:** a manual run from the Actions tab.
- **Push to `main`** touching one of these paths:
  - `renovate.json` and `renovate-config.json`, so a rule or runner change takes
    effect immediately rather than at the next scheduled run.
  - `.github/workflows/renovate.yml`, so a change to the workflow is exercised
    straight away.
  - `**/dotnet-tools.json`, the local tools manifest (`dotnet-openapi`, `clio`).
  - `Directory.Packages.props`, the Central Package Management file. Once a
    dependency PR merges, every other open one is a commit behind `main`. The
    push trigger lets Renovate rebase them immediately instead of leaving them
    conflicted until the next scheduled run.

Keep this path list in step with the config. A rule change in `renovate.json`
does not trigger a run unless `renovate.json` is in the list.

### Concurrency

```yaml
concurrency:
  group: renovate
  cancel-in-progress: false
```

Renovate mutates branches, and the push trigger makes overlapping runs likely.
A run already in progress may predate the merge that queued the next one, so
cancelling it could drop the rebase that was wanted. Runs queue instead.

## Automerge rules

Automerge is decided entirely by `packageRules` in `renovate.json`. Most rules
carry a `description` giving their reason; the Renovate action rule does not. The
current rules:

| Rule | Automerges |
|---|---|
| `renovatebot/github-action` (Renovate's own action) | All update types |
| Test infrastructure: `xunit*`, `Microsoft.NET.Test.*`, `Microsoft.Testing.*`, `NSubstitute*`, `AutoFixture*`, `coverlet*` | All update types, including majors |
| Build-time SDKs: `DotNet.ReproducibleBuilds*`, `Microsoft.SourceLink.*` | All update types |
| `WireMock.Net*` (sample app and tests only) | Minor and patch only |

Two exclusions are deliberate:

- **`Nerdbank.GitVersioning`** is not automerged, because it determines
  published version numbers.
- **Shipped runtime dependencies** (for example `Microsoft.Extensions.Http` and
  `Newtonsoft.Json`) match no automerge rule, so they need a human to merge.

All updates also wait seven days (`minimumReleaseAge: "7 days"`) before a PR is
opened.

Automerge only merges a PR once its required checks pass, and GitHub's
auto-merge feature must be enabled on the repository for that to happen. Renovate
merges through the API once the checks are green.

- <https://docs.renovatebot.com/key-concepts/automerge/>

## Troubleshooting

### Run fails before any PR is opened

The log shows a `401` or `403` from the GitHub API: check that `RENOVATE_TOKEN`
is set, has not expired, and has access to `sommmen/lightr`. Rotate the PAT
and update the secret if needed.

### Dependency PRs are not rebased after a merge

Check that the merged change touched a path listed under
[Workflow triggers](#workflow-triggers). A manifest that is not on that list (for
example a new `*.csproj` that adds a package) does not trigger a run until the
next scheduled one. Run the workflow manually with `workflow_dispatch` to pick
it up sooner.

### Config change has no effect

Run the validator above first. Then trigger the workflow with
`workflow_dispatch`, since a config edit that does not match a path filter will
not start a run on its own.

For the full decision trail, uncomment `LOG_LEVEL: "debug"` in the action's
`env:` block for one run. Re-comment it before merging.
