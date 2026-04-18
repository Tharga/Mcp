# Feature: Foundation (Phase 0)

## Goal

Establish `Tharga.Mcp` as the foundation package for MCP (Model Context Protocol) infrastructure in the Tharga ecosystem. Provide the contracts, registration pattern, and transport that every downstream provider package (`Tharga.MongoDB.Mcp`, `Tharga.Platform.Mcp`, etc.) and consuming application will build on. No dependency on any other Tharga package.

Cross-repo context: this feature delivers Phase 0 of the MCP super-feature. See `$DOC_ROOT/Tharga/plans/Mcp/plan.md`.

## Scope

**In scope:**
- Core contracts: `IMcpResourceProvider`, `IMcpToolProvider`, `IMcpContext`, `McpScope` enum
- Registration pattern: `IThargaMcpBuilder` with bundled-callback style (`services.AddThargaMcp(mcp => { ... })`)
- `AddMcp()` — DI registration, transport (HTTP + SSE via official `ModelContextProtocol` SDK), discovery
- `MapMcp()` — maps `/mcp/me`, `/mcp/team`, `/mcp/system` based on registered providers and their declared scope
- Wraps `ModelContextProtocol` 1.2.0 (latest stable)
- Unit tests
- Sample consumer (`samples/Tharga.Mcp.Sample`) with a hello-world provider callable from an external MCP client
- GitHub Actions CI/CD (build, test, pack, release/pre-release) using Tharga.Crawler as the reference
- README.md with usage documentation
- Target frameworks: `net8.0; net9.0; net10.0`

**Out of scope (later phases):**
- Any auth / scope / audit integration — that lives in `Tharga.Platform.Mcp` (Phase 1)
- Any provider implementation beyond the hello-world sample
- Tool-arg validation beyond what the MCP SDK gives for free (JSON Schema)
- `X-Team-Id` header parsing — wiring happens in `Tharga.Platform.Mcp`; this package just surfaces `TeamId` on `IMcpContext`
- Per-tool rate limiting, pagination, schema versioning, i18n

## Acceptance criteria

- `Tharga.Mcp` package builds on net8/9/10 without warnings beyond baseline
- All unit tests pass in `dotnet test -c Release`
- Sample project runs and responds to an MCP client (hello-world tool callable, hello-world resource readable)
- GitHub Actions workflow validates PRs and publishes to NuGet on merge to master
- README shows minimal consumer setup in under 20 lines

## Done condition

- Daniel has confirmed the sample works end-to-end from an external MCP client (e.g. Claude Code, `npx @modelcontextprotocol/inspector`)
- All acceptance criteria met
- README reflects final API shape
- Feature archived to `$DOC_ROOT/Tharga/plans/Mcp/done/foundation.md`
- PR merged to master via GitHub Actions CI

## Open questions / decisions

All open questions from the master plan were resolved before Phase 0 started (see `$DOC_ROOT/Tharga/plans/Mcp/plan.md` → "Open questions to resolve before Phase 0"). Decisions recorded:

- Repo: `c:\dev\tharga\Toolkit\Mcp\`, package name `Tharga.Mcp`
- Multi-team: `X-Team-Id` header (applied in Platform bridge, not here)
- Scope namespacing: hybrid (`mcp:*` for pure infrastructure, reuse existing scopes when mapping to a service)
- Validation: SDK (schema) + wrapper (auth-context, Phase 1) + provider (domain)
- Registration: bundled callback with `IThargaMcpBuilder`
- TFMs: `net8.0; net9.0; net10.0`
- SDK version: `ModelContextProtocol` 1.2.0
