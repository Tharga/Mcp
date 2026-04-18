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

### 5. MapMcp() endpoint mapping [~]
**Option A — single endpoint (decision 2026-04-18, master plan updated).**
Under `Tharga.Mcp/`:
- `MapMcp(this IEndpointRouteBuilder)` extension — maps a single MCP endpoint at `ThargaMcpOptions.EndpointBasePath` (default `/mcp`)
- Internally wires up the ModelContextProtocol SDK's `AddMcpServer().WithHttpTransport()` using the registered providers
- Bridges each registered `IMcpToolProvider`/`IMcpResourceProvider` into the SDK's tool/resource registry
- Scope carried via `IMcpContext.Scope`; endpoint itself does not filter by scope in Phase 0

Tests: endpoint present; tools/resources from registered providers discoverable via the SDK server.

### 6. Unit tests consolidation [ ]
Ensure coverage:
- Contracts
- Builder registrations (including double-registration idempotency)
- `AddMcp` DI graph
- `MapMcp` routing / scope filtering
- A fake provider exercised end-to-end through a `TestServer`

Run `dotnet test -c Release`. Fix any warnings over baseline.

### 7. Sample consumer [ ]
Under `samples/Tharga.Mcp.Sample/`:
- Minimal `Program.cs` using `AddThargaMcp(mcp => { mcp.AddResourceProvider<HelloWorldProvider>(); mcp.AddToolProvider<HelloWorldProvider>(); })` + `app.MapMcp()`
- `HelloWorldProvider` exposes one resource (`hello.greeting` → "hello world") and one tool (`hello.echo` echoing its input)
- README snippet in the sample showing how to run and connect with `npx @modelcontextprotocol/inspector`

Verify manually with MCP Inspector that list-resources, read-resource, list-tools, and call-tool all work.

### 8. GitHub Actions CI/CD [ ]
Copy `.github/workflows/build.yml` from `c:/dev/tharga/Toolkit/Crawler/.github/workflows/build.yml`. Adapt:
- `MAJOR_MINOR` = `0.1`
- Pack step: `dotnet pack src/Tharga.Mcp/Tharga.Mcp.csproj`
- `.NET` versions: `8.0.x`, `9.0.x`, `10.0.x`
- Warning threshold: 15 (default)
- Configure `NUGET_API_KEY` secret and `release` / `prerelease` environments in the repo on GitHub (ask Daniel to do this via UI before merging)

### 9. README.md [ ]
Overwrite the seed README with:
- One-paragraph "what is Tharga.Mcp"
- Install (`dotnet add package Tharga.Mcp`)
- Minimal usage example (10-20 lines)
- How to register a provider
- Link to the sample
- Badge row (NuGet version, build status) — add after first CI run

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

_(updated during implementation)_

## Last session

_(updated at end of session)_
