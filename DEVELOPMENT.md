# Development

Work yet to be done in this repository. Keep this list current as work is
planned, started, and finished.

## Todo

- [ ] Rename the CI job to `ci` (and update the README badge if the workflow
  file is renamed) after the `main-rs` ruleset requires `ci` instead of
  `build`; update the `renovate.json` rule descriptions that mention the
  required `build` check in the same change.
- [ ] Decide whether to enable `TreatWarningsAsErrors`, including how to handle
  the transitive `NU1902` advisory (`Microsoft.AspNetCore.WebUtilities` via
  WireMock.Net in the sample projects).
- [ ] Refresh [docs/xunit-v3-mtp-v2-baseline.md](docs/xunit-v3-mtp-v2-baseline.md);
  it still lists older xunit/GitHubActionsReport versions and no `global.json`.