# Plan: Foundation (Phase 0)

Branch: `feature/foundation`
Feature: see `plan/feature.md`

## Steps

### 1. Solution + project skeleton [x]
Layout mirrors Tharga.Crawler (no `src/`/`tests/`/`samples/` parent folders).
Created:
- `Tharga.Mcp.sln` at repo root (classic .sln format — `dotnet new sln --format sln`, since current .NET SDK defaults to .slnx which the rest of Tharga isn't using yet)
- `Tharga.Mcp/Tharga.Mcp.csproj` — library, TFMs `net8.0; net9.0; net10.0`, PackageId `Tharga.Mcp` v0.1.0, `Microsoft.AspNetCore.App` FrameworkReference, references `ModelContextProtocol` 1.2.0 + `ModelContextProtocol.AspNetCore` 1.2.0
- `Tharga.Mcp.Tests/Tharga.Mcp.Tests.csproj` — xUnit v2 test project (net10.0, mirrors Crawler.Tests packages)
- `Sample/Tharga.Mcp.Sample/Tharga.Mcp.Sample.csproj` — minimal ASP.NET Core web app, net10.0

Build (`dotnet build -c Release`) and test (`dotnet test -c Release`) both pass, 0 warnings. Placeholder test verifies the skeleton compiles.

### 2. Core contracts [x]
Added (flat layout at `Tharga.Mcp/` root, matching Crawler style):
- `McpScope` — User/Team/System
- `IMcpContext` — UserId, TeamId, IsDeveloper, Scope
- `IMcpProvider` base + `IMcpResourceProvider` + `IMcpToolProvider` (each declares a single `Scope`)
- Descriptor records: `McpResourceDescriptor`, `McpResourceContent`, `McpToolDescriptor`, `McpToolResult`, `McpContent`

**Design decision:** providers declare one scope each (not a set). For apps that serve multiple scopes (e.g. PlutusWave), the consumer registers multiple provider classes. Forces clean separation and avoids every provider method taking a scope argument.

**SDK alignment note:** the MCP C# SDK is attribute-based (`[McpServerTool]`). Our `IMcp*Provider` interfaces are Tharga-level abstractions; step 4/5 will bridge them to the SDK by wiring each provider's methods into the SDK's tool registry at server-build time. This keeps providers declarative and the SDK as an implementation detail.

Tests (4, all passing):
- `McpScopeTests.Scope_has_three_levels`
- `ProviderContractTests.Resource_provider_declares_scope_and_surfaces_contents`
- `ProviderContractTests.Resource_provider_returns_declared_resources_and_content`
- `ProviderContractTests.Tool_provider_returns_declared_tools_and_echoes_arguments`

### 3. IThargaMcpBuilder + options [x]
Added:
- `ThargaMcpOptions` — `EndpointBasePath` (default `/mcp`), `RequireAuth` placeholder (Phase 1 bridges)
- `IThargaMcpBuilder` — `Services`, `Options`, `AddResourceProvider<T>`, `AddToolProvider<T>`
- `Internal/McpProviderRegistration`, `McpProviderRegistry`, `ThargaMcpBuilder` (concrete, internal)

Design notes:
- Registry dedupes by implementation type (idempotent) — same pattern Cache adopted after the 2026-04 fix
- `AddResourceProvider<T>` registers both the concrete type (`TryAddTransient<T>()`) and a factory for `IMcpResourceProvider` resolving to T, so `IEnumerable<IMcpResourceProvider>` enumerates all providers
- Registry is internal; consumers only touch the builder

Tests (5): idempotent registration on duplicate calls; resolvability via `IEnumerable<IMcp*Provider>`; options mutation visible; multiple distinct providers both register.

### 4. AddMcp() entry point [x]
Added:
- `ThargaMcpServiceCollectionExtensions.AddThargaMcp(IServiceCollection, Action<IThargaMcpBuilder>)` — top-level entry point matching the master plan's signature
- `IMcpContextAccessor` + `Internal.McpContextAccessor` (AsyncLocal-backed), registered as a singleton
- Idempotency via `GetOrCreateSingleton` — second call reuses the same registry/options instance and merges registrations

Deferred to step 5:
- Actual MCP SDK server wiring (`AddMcpServer`, `WithHttpTransport`, `WithTools`) — that's per-endpoint, so it belongs in `MapMcp`
- `IMcpContext` instance construction — the transport pipeline sets it via the accessor before calling providers

Tests (5): callback receives the builder; accessor is singleton + AsyncLocal-flow; calling twice merges registrations and preserves the same registry/options.

### 4b. Remove nullable reference types [x]
Removed `<Nullable>enable</Nullable>` from all three csprojs. Stripped `?` from reference-type annotations in `IMcpContext`, `IMcpContextAccessor`, `McpContent`, `McpResourceContent`, `McpResourceDescriptor`, `McpToolDescriptor`, `Internal/McpContextAccessor`, and the two test files' fake context records. Kept `JsonElement?` (value-type nullable). Build clean (0 warnings), 14 tests pass.

Feedback saved to memory: no nullable reference types in any Tharga .csproj.

### 5. MapMcp() endpoint mapping [x]
**Option A — single endpoint (decision 2026-04-18, master plan updated).**
- `AddThargaMcp` now also calls the SDK's `AddMcpServer().WithHttpTransport()` so the MCP server is ready
- `MapMcpExtensions.MapMcp(this IEndpointRouteBuilder)` reads `ThargaMcpOptions.EndpointBasePath` and delegates to the SDK's `MapMcp(path)` overload
- Scope carried via `IMcpContext.Scope`; endpoint itself does not filter by scope in Phase 0

**Deferred to a follow-up** — bridging `IMcpToolProvider`/`IMcpResourceProvider` registrations into SDK-side tools/resources. The SDK creates tools from delegates/methods (with schema inferred from the signature) — translating our `JsonElement`-based dynamic contract would either leak SDK types into providers or hand-roll schema generation. Deferring until Phase 2 (first real provider, MongoDB) gives us a concrete tool shape to design against. For Phase 0, the sample project will register tools via the SDK's `[McpServerTool]` attribute using `mcp.Services.AddMcpServer().WithTools<T>()` — that path works today because `AddThargaMcp` wires the SDK server.

Tests (2): endpoint at default path is mapped; endpoint at a configured path is mapped and the old path returns 404.

### 6. Unit tests consolidation [x]
16 tests cover:
- Contracts (scope enum + provider shape) — 4
- Builder registrations and idempotency — 5
- `AddThargaMcp` DI graph + AsyncLocal accessor — 5
- `MapMcp` endpoint wired at default and configured paths — 2

Build clean (0 warnings), all tests pass. End-to-end provider-through-TestServer is deferred alongside the provider→SDK bridge (step 5 notes).

### 7. Sample consumer [x]
Under `Sample/Tharga.Mcp.Sample/`:
- `Program.cs` — minimal ASP.NET Core web app: `AddThargaMcp(mcp => { mcp.Services.AddMcpServer().WithTools<HelloTools>(); })` + `app.MapMcp()`
- `HelloTools` — two `[McpServerTool]` attributed methods: `greet(name)` and `echo(message)`
- Provider-style registration via `mcp.AddToolProvider<T>()` is available in the builder but deferred (bridge lands with Phase 2 MongoDB); attribute-based tools are the Phase 0 path

**Manual end-to-end verification (localhost:5138):**
- `initialize` → `{"name":"Tharga.Mcp.Sample","version":"1.0.0.0"}` + `tools.listChanged`
- `tools/list` → both tools discovered with correct JSON schemas (description, required params)
- `tools/call greet {"name":"Daniel"}` → `"Hello, Daniel!"`

Meets Phase 0's acceptance criterion: a hello-world tool can be called from an external MCP client.

### 8. GitHub Actions CI/CD [x]
Copied `.github/workflows/build.yml` from Tharga.Crawler. Adapted:
- `MAJOR_MINOR` = `0.1`
- Pack step: `dotnet pack Tharga.Mcp/Tharga.Mcp.csproj`
- `.NET` versions: `8.0.x`, `9.0.x`, `10.0.x`
- Warning threshold: 15 (default)

**Before the first merge to master, Daniel must configure on GitHub:**
- `NUGET_API_KEY` secret
- `release` and `prerelease` environments
- `CODECOV_TOKEN` secret (optional — codecov step fails soft)

**Known minor inheritance from Crawler's workflow:** the pack step in the build job always uses the stable version, so on PR runs the uploaded artifact ships with the stable version and the prerelease job publishes it under a pre-release tag — visible as a version mismatch between the GitHub Release title and the .nupkg filename. Not blocking for Phase 0 but worth fixing upstream later; flagging so we notice if/when it surfaces.

### 9. README.md [x]
Overwrote the seed README with:
- One-paragraph "what is Tharga.Mcp"
- Install snippet
- Minimal usage example (10 lines)
- Attribute-based tool definition
- Provider-style composition pattern for future packages
- Endpoint-scope note (single endpoint for Phase 0, decision link)
- Sample run + MCP Inspector connection
- License
- Badge row deferred until the first CI run produces a NuGet version

## Commit milestones

- After step 1: "feat: project skeleton"
- After step 2: "feat: core contracts"
- After step 3: "feat: IThargaMcpBuilder registration"
- After step 4: "feat: AddThargaMcp entry point"
- After step 5: "feat: MapMcp three-level routing"
- After step 6: "test: foundation coverage pass"
- After step 7: "feat: hello-world sample"
- After step 8: "ci: GitHub Actions workflow"
- After step 9: "docs: README"

Run `dotnet build -c Release` and `dotnet test -c Release` before each commit. Never commit failing tests.

## Progress log

All nine steps complete. Feature is implementation-ready for review.

## Last session

Completed Phase 0 foundation end-to-end in one sitting:
- Steps 1–9 all done (skeleton → contracts → builder → AddThargaMcp → step 4b nullable removal → MapMcp → test consolidation → sample → CI/CD → README)
- 16 unit tests, 0 warnings, end-to-end MCP initialize/list/call verified against a running sample (greet returned "Hello, Daniel!")
- Master plan updated with the single-endpoint decision (2026-04-18); `IMcp*Provider`→SDK bridge deferred to Phase 2 when the first real provider arrives
- Feedback saved to memory: no `<Nullable>enable</Nullable>` in Tharga projects

**Ready for Daniel to review and test.** Outstanding before closing the feature:
- Configure `NUGET_API_KEY`, `CODECOV_TOKEN`, and `release`/`prerelease` environments on GitHub
- Push `feature/foundation` and open a PR to `master`
- Validate CI runs green, first NuGet publish works
- Then close per "Completing implementation" in shared-instructions (Daniel confirms → archive feature.md → final commit)

Known deferred items to carry into a follow-up:
- Three-endpoint split (`/mcp/me`/`/mcp/team`/`/mcp/system`) — revisit in Phase 1 or when SDK gains native support
- `IMcpToolProvider`/`IMcpResourceProvider`→SDK-tool bridge — design when MongoDB provider is built
- GitHub Actions pre-release version mismatch in the artifact (inherited from Crawler) — fix upstream later
