# Feature: rename-use-tharga-mcp

## Goal

Rename `MapMcp(IEndpointRouteBuilder)` to `UseThargaMcp(IEndpointRouteBuilder)` to match the Tharga ecosystem's `AddThargaXxx` / `UseThargaXxx` convention used by every other package (Cache, MongoDB, Platform, etc.). Keep the old name as `[Obsolete]` forwarder for one release cycle so no consumer breaks at the point of upgrade.

Tracks the request in `$DOC_ROOT/Tharga/Requests.md` → `## Tharga.Mcp` → *"Rename MapMcp to UseThargaMcp"* (2026-04-19, from Tharga.MongoDB).

## Scope

**In scope:**
- Add `UseThargaMcp(this IEndpointRouteBuilder endpoints)` returning `IEndpointConventionBuilder`. File renamed `MapMcpExtensions.cs` → `UseThargaMcpExtensions.cs`.
- Keep `MapMcp()` as `[Obsolete("Use UseThargaMcp() instead. MapMcp will be removed in a future version.")]` forwarder that delegates to `UseThargaMcp()`.
- Update all consumer call sites inside this repo:
  - `Sample/Tharga.Mcp.Sample/Program.cs`
  - `Tharga.Mcp.Tests/Routing/MapMcpTests.cs` → rename file to `UseThargaMcpTests.cs`, update calls
  - `Tharga.Mcp.Tests/Bridge/ProviderBridgeTests.cs` → update calls
- Update README minimal-usage example
- Close the Requests.md entry with Follow-up for MongoDB.Mcp (combine with the bridge follow-up where possible)

**Out of scope:**
- Removing the obsolete forwarder — that waits for a future minor bump
- Any `MAJOR_MINOR` bump — additive change, next CI auto-patch

## Acceptance criteria

- `UseThargaMcp()` works exactly like `MapMcp()` did — same endpoint, same behavior
- `MapMcp()` still compiles + works but produces a CS0618 obsolete warning
- All 22 existing tests still pass
- Sample runs and responds to MCP clients using `UseThargaMcp()`
- No build warnings in the library itself (obsolete calls inside the library are suppressed if any)
- README shows `UseThargaMcp()` in the minimal example

## Done condition

- PR merged to master via GitHub Actions CI
- Requests.md entry marked Done
- `plan/` archived to `$DOC_ROOT/Tharga/plans/Mcp/done/rename-use-tharga-mcp.md`
