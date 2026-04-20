# Plan: auto-authorization

Branch: `feature/auto-authorization`
Feature: see `plan/feature.md`

## Steps

### 1. UseThargaMcp reads RequireAuth [x]
- `UseThargaMcp` calls `.RequireAuthorization()` on the convention builder when `options.RequireAuth == true`
- xmldoc updated to describe the default behavior and how to opt out
- **Caveat discovered during testing:** endpoints with auth metadata throw `ThrowMissingAuthMiddlewareException` at request time if `UseAuthorization()` isn't in the pipeline. This makes the change a behavior shift for consumers who have `RequireAuth = true` but no auth middleware wired. For this repo: sample and tests didn't have auth wired, so they now explicitly set `RequireAuth = false`. Production consumers that want auth (the target audience of the request) already have the middleware wired — this is intentional.

### 2. Tests [x]
- `RequireAuth_true_adds_AuthorizeAttribute_metadata_to_the_endpoint` — inspects `EndpointDataSource` endpoints filtered to `/mcp`, asserts every endpoint has `IAuthorizeData` metadata
- `RequireAuth_false_does_not_add_AuthorizeAttribute_metadata_to_the_endpoint` — same shape, negative assertion
- Updated existing test helpers (`UseThargaMcpTests.BuildHostAsync`, `ProviderBridgeTests.BuildHostAsync`) to default `RequireAuth = false` to avoid the missing-auth-middleware exception
- Sample `Program.cs` also sets `RequireAuth = false` with an explanatory comment
- 27/27 tests pass

### 3. README update [x]
- "Minimal usage" section now explains `RequireAuth` default, that it requires `UseAuthorization()` in the pipeline, and how to opt out.

### 4. Close Requests.md entry + Follow-up [ ]
- Mark Done with summary
- Follow-up for Tharga.Platform.Mcp: drop `MapMcpPlatform()` wrapper once they upgrade

### 5. Close feature [ ]
- Archive + delete plan/
- Final commit

## Commit milestones

- After step 2: `feat: UseThargaMcp applies RequireAuthorization when RequireAuth is true`
- After step 3: `docs: README notes RequireAuth is honored by UseThargaMcp`
- After step 4: `chore: close auto-authorization request`
- After step 5: `feat: auto-authorization complete`

## Progress log

_(updated during implementation)_

## Last session

_(updated at end of session)_
