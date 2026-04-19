# Feature: provider-bridge

## Goal

Make the `IMcpToolProvider` / `IMcpResourceProvider` contracts that shipped in Phase 0 actually functional — dispatch their methods from the MCP SDK so consumers (Tharga.MongoDB.Mcp and future provider packages) work end-to-end without having to register anything through the SDK's attribute pattern.

Phase 0 intentionally deferred this bridge until a concrete provider existed to design against. That provider now exists in `Tharga.MongoDB.Mcp` (Phase 2 of the master plan) — and is blocked today with `Method 'resources/list' is not available`.

Tracks request in `$DOC_ROOT/Tharga/Requests.md` → `## Tharga.Mcp` → *"Bridge IMcpToolProvider / IMcpResourceProvider into the SDK dispatch"* (2026-04-19, from Tharga.MongoDB).

## Scope

**In scope:**
- Internal `McpProviderDispatcher` that enumerates DI-registered providers and plugs into the SDK's server options:
  - `Capabilities.Tools.ListToolsHandler` → aggregates `IMcpToolProvider.ListToolsAsync(...)` from all registered providers
  - `Capabilities.Tools.CallToolHandler` → dispatches to the owning `IMcpToolProvider.CallToolAsync(...)` based on tool name
  - `Capabilities.Resources.ListResourcesHandler` → aggregates `IMcpResourceProvider.ListResourcesAsync(...)`
  - `Capabilities.Resources.ReadResourceHandler` → dispatches to the owning `IMcpResourceProvider.ReadResourceAsync(...)` based on URI
- Scope filtering: provider is only consulted if its `Scope` matches `IMcpContextAccessor.Current.Scope`. When `Current` is `null` (no Phase 1 bridge yet) → no filtering, all providers visible.
- Coexist with SDK attribute-based tools (`.WithTools<T>()`) — both work additively in the same server
- Tests: TestServer round-trips for tools and resources, scope-filter test, coexistence test with an attribute-based tool
- Sample update: add one `IMcpToolProvider`-based tool alongside `HelloTools`
- README: document the provider-style registration example
- Close the Requests.md entry once verified

**Out of scope:**
- Phase 1 auth/scope wiring — the dispatcher uses whatever `IMcpContextAccessor.Current` returns; actually populating that is Phase 1's job
- Three-endpoint split — still deferred
- Prompts primitive — Tharga contracts don't model prompts yet; separate feature if needed

## Acceptance criteria

- A fake `IMcpToolProvider` registered via `mcp.AddToolProvider<T>()` is:
  - Discoverable via `tools/list`
  - Callable via `tools/call` with the same semantics as an attribute-based tool
- Same for `IMcpResourceProvider`
- Attribute-based tools still work alongside (existing sample's `HelloTools` keeps passing)
- All existing tests still pass, new tests cover the bridge
- `Tharga.MongoDB.Mcp`'s existing providers work against the next `Tharga.Mcp` release without any code change on their side — verified manually against a MongoDB sample

## Done condition

- All acceptance criteria met
- Daniel confirms Tharga.MongoDB.Mcp works end-to-end after upgrading
- Requests.md entry marked Done with a Follow-up line for consumers to upgrade
- README reflects final API
- Feature archived to `$DOC_ROOT/Tharga/plans/Mcp/done/provider-bridge.md`
- PR merged to master via GitHub Actions CI
