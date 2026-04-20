# Feature: scope-hierarchy

## Goal

Fix the Tharga.Mcp side of a production-blocking bug where the dispatcher's strict-equality scope filter, combined with `Tharga.Platform.Mcp`'s hardcoded `McpScope.User` in the context accessor, hides every System-scope provider (notably `Tharga.MongoDB.Mcp`) from authenticated `/mcp` callers. Restores the README's stated "union of registered tools and resources" semantics for a fully-privileged caller.

Tracked in `$DOC_ROOT/Tharga/Requests.md` → `## Tharga.Mcp` → *"Dispatcher scope filter + accessor hardcoded User scope hide System providers from /mcp"* (2026-04-20, from Quilt4Net Server + PlutusWave).

## Scope

**In scope (this repo):**
- `McpProviderDispatcher.Resolve<T>` — change `p.Scope == current.Scope` to `p.Scope <= current.Scope`. Enum ordering (`User = 0`, `Team = 1`, `System = 2`) turns this into a hierarchy filter: System caller sees everything; Team sees User + Team; User sees only User.
- Inline xmldoc on the dispatcher updated to describe hierarchy semantics.
- New test `System_scope_caller_sees_providers_from_all_scopes`.
- Existing `When_accessor_current_is_set_only_matching_scope_providers_are_visible` test renamed to reflect hierarchy (`User_scope_caller_sees_only_user_scope_providers`) — same assertions still hold since User ≤ User still matches and System > User.
- README — update the endpoint-scopes section from *"scope is carried on `IMcpContext.Scope` and enforced by providers"* to say the dispatcher applies a hierarchy filter based on `IMcpContext.Scope`.
- Close the Requests.md entry (mark Done, summary).
- File a **new** Requests.md entry under `## Tharga.Platform` for the `Tharga.Platform.Mcp` side (`HttpContextMcpContextAccessor` should compute scope from claims, not hardcode `User`).
- Add Requests.md Follow-ups for Quilt4Net Server + PlutusWave to upgrade both packages.

**Out of scope:**
- The Platform.Mcp fix itself — lives in `c:\dev\tharga\Toolkit\Platform`, cross-project guard applies. Surfaced via the new Requests.md entry.
- The separate auto-`RequireAuthorization` request (Medium priority) — later feature.

## Acceptance criteria

- System caller sees providers across all scopes; Team caller sees User + Team; User caller sees only User (new test covers System; existing test covers User).
- Build clean; all existing tests pass (the behavior change is additive for non-User scopes — no existing test expected a System caller to see no System providers).
- README and xmldoc reflect hierarchy semantics.
- Tharga.Mcp Requests.md entry marked Done; Platform-side entry filed Pending; consumer follow-ups added.

## Done condition

- PR merged via CI
- Close the feature per shared-instructions
