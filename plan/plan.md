# Plan: rename-use-tharga-mcp

Branch: `feature/rename-use-tharga-mcp`
Feature: see `plan/feature.md`

## Steps

### 1. Rename the extension class and method [x]
- `Tharga.Mcp/MapMcpExtensions.cs` → `Tharga.Mcp/UseThargaMcpExtensions.cs`
- Primary extension: `UseThargaMcp(this IEndpointRouteBuilder)` — body moved from old `MapMcp`
- Obsolete forwarder: `[Obsolete("...")] MapMcp(this IEndpointRouteBuilder)` → delegates to `UseThargaMcp()`
- Build succeeds; 3 CS0618 warnings emitted exactly where expected (Program.cs + 2 test files still on `MapMcp`) — step 2 clears these.

### 2. Update in-repo call sites [x]
- `Sample/Tharga.Mcp.Sample/Program.cs`: switched to `app.UseThargaMcp()`
- `Tharga.Mcp.Tests/Routing/MapMcpTests.cs` → `UseThargaMcpTests.cs` (file + class renamed; all tests use `UseThargaMcp()`)
- `Tharga.Mcp.Tests/Bridge/ProviderBridgeTests.cs`: switched
- New test `Obsolete_MapMcp_alias_still_maps_the_endpoint` proves backward compat (wraps the call in `#pragma warning disable CS0618`)
- Build clean (0 warnings), 23/23 tests pass (22 original + 1 forwarder test)

### 3. Update README [x]
- Minimal usage example uses `app.UseThargaMcp()`
- Note added about the `[Obsolete]` `MapMcp()` alias and its removal timeline

### 4. Build + test [x]
- Build clean (0 warnings) + tests green at 23/23 after step 2.
- Sample-run verification skipped — the existing end-to-end bridge tests already exercise the MCP HTTP roundtrip through `UseThargaMcp()`, same plumbing the sample uses.

### 5. Close the Requests.md entry [x]
- Marked "Rename MapMcp to UseThargaMcp" Done (2026-04-19) with a one-liner summary of what shipped.
- Follow-up added: *"Tharga.MongoDB.Mcp should switch `app.MapMcp()` → `app.UseThargaMcp()` at next Tharga.Mcp upgrade"*.

### 6. Close the feature [ ]
- Archive `plan/feature.md` + `plan/plan.md` to `$DOC_ROOT/Tharga/plans/Mcp/done/rename-use-tharga-mcp.md`
- `git rm -r plan/`
- Final commit: `feat: rename-use-tharga-mcp complete`

## Commit milestones

- After step 1: "feat: add UseThargaMcp, mark MapMcp obsolete"
- After step 2: "refactor: use UseThargaMcp across sample and tests"
- After step 3: "docs: README uses UseThargaMcp"
- After step 5: "chore: close rename-use-tharga-mcp request"
- After step 6: "feat: rename-use-tharga-mcp complete"

Run `dotnet build -c Release` and `dotnet test -c Release` before each commit.

## Progress log

_(updated during implementation)_

## Last session

_(updated at end of session)_
