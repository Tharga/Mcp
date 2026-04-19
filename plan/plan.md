# Plan: prerelease-version-fix

Branch: `feature/prerelease-version-fix`
Feature: see `plan/feature.md`

## Steps

### 1. Fix build.yml (Option A) [x]
Replaced the single `Pack` step with two conditional steps (lines 98-106). Basic sanity check: no tabs, both new steps present, `steps.preversion.outputs.version` reference present.

### 2. Syntax validate [x]
Python YAML module not installed, but a structural check passes: no tabs, both new steps present, preversion ref present. Trigger matrix reviewed — `push` to master → stable pack only; `pull_request` → prerelease pack only; `push` to any other ref (none expected) → no pack, artifact upload would fail, visible immediately.
- `python -c "import yaml; yaml.safe_load(open('.github/workflows/build.yml'))"` (or equivalent)
- Review the conditional logic one more time against the trigger matrix

### 3. File per-project Requests.md entries [x]
Added 5 entries:
- `## Tharga.Wpf` (line 333) — Review
- `## Tharga.Crawler` (line 344, **new section**) — Fix (also reference template — note about propagation)
- `## Tharga.Blazor` (line 357, **new section**) — Review
- `## Quilt4Net.Toolkit` (line 152) — Review (note: 4 `dotnet pack` lines)
- `## Tharga.MongoDB` (line 400) — Review
One entry under each of: Tharga.Crawler, Tharga.MongoDB, Tharga.Wpf, Tharga.Blazor, Quilt4Net.Toolkit. Same template — from Tharga.Mcp, priority Medium, description with the flow trace, proposed fix (Option A). For Crawler, note that it's the reference template source.

### 4. Close the feature [ ]
- Archive `plan/feature.md` + `plan/plan.md` to `$DOC_ROOT/Tharga/plans/Mcp/done/prerelease-version-fix.md`
- `git rm -r plan/`
- Final commit `feat: prerelease-version-fix complete`

## Commit milestones

- After step 1: `fix(ci): pack with correct version for PR vs master push`
- After step 3: `chore: request CI review for repos that forked Crawler's workflow`
- After step 4: `feat: prerelease-version-fix complete`

## Progress log

_(updated during implementation)_

## Last session

_(updated at end of session)_
