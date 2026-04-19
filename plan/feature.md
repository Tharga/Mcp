# Feature: prerelease-version-fix

## Goal

Fix a latent bug in `Tharga.Mcp`'s GitHub Actions workflow (inherited from Tharga.Crawler's reference template) that would publish the **stable** version to NuGet from a PR's prerelease run. Then file per-project requests for every repo that already forked Crawler's workflow so each owner can review their own copy.

## The bug (background)

In `build` job:
- `Compute version` → `steps.version.outputs.version` = stable (e.g. `0.1.2`)
- `Compute pre-release version` → `steps.preversion.outputs.version` = pre-release (e.g. `0.1.2-pre.1`)
- `Pack` → `dotnet pack ... -p:PackageVersion=${{ steps.version.outputs.version }}` ← **always stable**

In `prerelease` job (PR-triggered):
- Downloads artifact (contains `Tharga.Mcp.0.1.2.nupkg` — stable version baked into the .nuspec)
- Pushes to NuGet → **stable version `0.1.2` gets published from a PR**
- Creates GitHub Release tagged `0.1.2-pre.1` with the stable-versioned .nupkg attached

`--skip-duplicate` means whichever PR runs first wins; stable release of that patch goes to NuGet before the PR is even merged. Not damaging today (the `prerelease` environment requires manual approval and you haven't approved one yet), but latent.

## Scope

**In scope:**
- Fix `.github/workflows/build.yml` with **Option A**: two conditional pack steps (stable on master push, pre-release on PR)
- File requests in `$DOC_ROOT/Tharga/Requests.md` under each consumer's section:
  - Tharga.Crawler (also the template source — fixing here stops future propagation)
  - Tharga.MongoDB
  - Tharga.Wpf
  - Tharga.Blazor
  - Quilt4Net.Toolkit

**Out of scope:**
- Pending-migration projects (Depend, Toolkit, Console, Fortnox, Reporter, Runtime, Test, Communication) — they'll pick up the fixed template when they migrate, assuming Crawler's owner fixes the reference.
- Fixing Crawler's workflow itself — cross-project work; handled via the Requests.md entry.

## Acceptance criteria

- Tharga.Mcp's `build.yml` packs with the correct version for its context (stable on push-to-master, pre-release on PR)
- `yamllint` / syntax check passes
- Requests.md has five new "Pending" entries, one per consumer
- All existing tests still pass; nothing in the library changes

## Done condition

- PR merged
- Close the feature per shared-instructions
