# Feature: auto-authorization

## Goal

Honor `ThargaMcpOptions.RequireAuth` inside `UseThargaMcp(IEndpointRouteBuilder)` so the flag consumers set at registration actually does something. Today the flag is parked, waiting for the Platform bridge; consumers who want auth have to chain `.RequireAuthorization()` themselves or depend on `Tharga.Platform.Mcp`'s `MapMcpPlatform()` wrapper. After this change, `AddThargaMcp(mcp => mcp.Options.RequireAuth = true)` alone is enough and `Platform.Mcp` can drop its wrapper in favor of plain `app.UseThargaMcp()`.

Tracked in `$DOC_ROOT/Tharga/Requests.md` → `## Tharga.Mcp` → *"UseThargaMcp should automatically apply RequireAuthorization when ThargaMcpOptions.RequireAuth is true"* (2026-04-20, from Tharga.Platform).

## Scope

**In scope:**
- `UseThargaMcp` reads `ThargaMcpOptions.RequireAuth`. When `true`, calls `.RequireAuthorization()` on the returned `IEndpointConventionBuilder`. When `false`, returns the builder unchanged.
- xmldoc on `UseThargaMcp` updated to describe this behavior.
- Tests:
  - `RequireAuth_true_adds_AuthorizeAttribute_metadata` — build host with `RequireAuth = true`, inspect endpoint metadata
  - `RequireAuth_false_does_not_add_AuthorizeAttribute_metadata`
- README minimal-usage section — short note.
- Mark Requests.md entry Done with summary.
- Follow-up: `Tharga.Platform.Mcp` should drop `MapMcpPlatform()` in favor of plain `app.UseThargaMcp()` once they upgrade.

**Out of scope:**
- Changing the default value of `RequireAuth` — stays `true` per the request rationale.
- Adding authentication/authorization schemes — consumers wire those themselves (the default policy just requires an authenticated user).
- Touching the sample — `UseAuthorization()` isn't in the sample's pipeline, so `.RequireAuthorization()` is metadata-only there (harmless).
- Touching existing tests — same reason, no `UseAuthorization()` in their test pipelines, no behavior change.

## Acceptance criteria

- `UseThargaMcp()` at default options (`RequireAuth = true`) produces an endpoint with `AuthorizeAttribute` metadata.
- Setting `mcp.Options.RequireAuth = false` produces an endpoint **without** `AuthorizeAttribute`.
- Existing 25 tests still pass — no silent behavior change for test pipelines (which don't have `UseAuthorization()`).
- Build clean on net8/9/10.

## Done condition

- PR merged via CI
- Close feature per shared-instructions
