# Plan: rename-use-tharga-mcp

Branch: `feature/rename-use-tharga-mcp`
Feature: see `plan/feature.md`

## Steps

### 1. Rename the extension class and method [x]
- `Tharga.Mcp/MapMcpExtensions.cs` → `Tharga.Mcp/UseThargaMcpExtensions.cs`
- Primary extension: `UseThargaMcp(this IEndpointRouteBuilder)` — body moved from old `MapMcp`
- Obsolete forwarder: `[Obsolete("...")] MapMcp(this IEndpointRouteBuilder)` → delegates to `UseThargaMcp()`
- Build succeeds; 3 CS0618 warnings emitted exactly where expected (Program.cs + 2 test files still on `MapMcp`) — step 2 clears these.

### 2. Update in-repo call sites [ ]
- `Sample/Tharga.Mcp.Sample/Program.cs`: `app.MapMcp()` → `app.UseThargaMcp()`
- `Tharga.Mcp.Tests/Routing/MapMcpTests.cs` → rename file + class to `UseThargaMcpTests`; calls switch to `UseThargaMcp()`
- `Tharga.Mcp.Tests/Bridge/ProviderBridgeTests.cs`: `endpoints.MapMcp()` → `endpoints.UseThargaMcp()`
- Add one new test that exercises the `[Obsolete]` `MapMcp()` forwarder to prove backward compat

### 3. Update README [ ]
- Minimal usage example: `app.MapMcp()` → `app.UseThargaMcp()`
- Short note about the obsolete alias for existing consumers

### 4. Build + test [ ]
- `dotnet build -c Release` — 0 warnings
- `dotnet test -c Release` — all passing (22 original + 1 forwarder test)
- Run sample and verify endpoint still responds

### 5. Close the Requests.md entry [ ]
- Mark the "Rename MapMcp to UseThargaMcp" request Done (2026-04-19) with summary
- Follow-up: "Tharga.MongoDB.Mcp should switch to UseThargaMcp() at next Tharga.Mcp upgrade"

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
