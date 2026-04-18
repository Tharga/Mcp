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

### 2. Core contracts [~]
Under `src/Tharga.Mcp/Contracts/`:
- `McpScope` enum — `User`, `Team`, `System`
- `IMcpContext` — `UserId`, `TeamId`, `IsDeveloper`, `Scope` (McpScope)
- `IMcpResourceProvider` — surface resources (read-only data) for a given scope
- `IMcpToolProvider` — surface tools (callable actions) for a given scope
- Shape provider interfaces so they can declare supported scopes and return typed resource/tool descriptors

Tests: contract shape tests (e.g. a fake provider declaring `McpScope.System` is visible to system discovery only).

### 3. IThargaMcpBuilder + options [ ]
Under `src/Tharga.Mcp/Builder/`:
- `IThargaMcpBuilder` — access to `IServiceCollection`, endpoint path config (default `/mcp`), `RequireAuth` flag, provider registration helpers (`AddResourceProvider<T>`, `AddToolProvider<T>`)
- `ThargaMcpOptions` — resolvable options record for downstream wiring
- `AddResourceProvider` / `AddToolProvider` extension methods on the builder

Tests: builder records provider registrations; options resolved from DI match configured values.

### 4. AddMcp() entry point [ ]
Under `src/Tharga.Mcp/DependencyInjection/`:
- `AddThargaMcp(this IServiceCollection, Action<IThargaMcpBuilder>)` — the top-level entry point referenced in the master plan
- Wires up `ModelContextProtocol` SDK services (transport, discovery)
- Registers `IMcpContext` accessor with a default no-auth implementation (Platform bridge will replace in Phase 1)

Tests: calling `AddThargaMcp` twice is idempotent (merge, not throw — same pattern as `AddCache` after the Cache fix); required services resolvable after registration.

### 5. MapMcp() endpoint mapping [ ]
Under `src/Tharga.Mcp/Routing/`:
- `MapMcp(this IEndpointRouteBuilder)` extension
- Maps three endpoints based on whether any registered provider supports that scope:
  - `/mcp/me` — only if a provider declares `McpScope.User`
  - `/mcp/team` — only if a provider declares `McpScope.Team`
  - `/mcp/system` — only if a provider declares `McpScope.System`
- Each endpoint bridges to the ModelContextProtocol SDK transport for HTTP + SSE
- Endpoint filter selects providers matching the endpoint's scope when fulfilling discovery/invocation

Tests: endpoint existence matches registered scopes; provider resource/tool lists filtered per endpoint scope.

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
