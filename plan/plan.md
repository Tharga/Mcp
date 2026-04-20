# Plan: scope-hierarchy

Branch: `feature/scope-hierarchy`
Feature: see `plan/feature.md`

## Steps

### 1. Dispatcher filter + xmldoc [x]
- `McpProviderDispatcher.Resolve<T>`: `p.Scope == current.Scope` → `p.Scope <= current.Scope`
- Added xmldoc on `Resolve<T>` describing hierarchy semantics (System 2 > Team 1 > User 0)

### 2. Tests [x]
- Renamed existing test → `User_scope_caller_sees_only_user_scope_providers`
- Added `Team_scope_caller_sees_user_and_team_providers_but_not_system`
- Added `System_scope_caller_sees_providers_from_all_scopes`
- Added `TeamScopeTool` fake
- Extracted common middleware/list-names setup into `ListToolNamesUnderScopeAsync` helper
- Full suite: 25/25 (was 23 + 2 new hierarchy tests)

### 3. README update [x]
- Endpoint-scopes section rewritten to describe the hierarchy: System → User+Team+System, Team → User+Team, User → User only. Also mentions anonymous/null context behavior and where the effective scope comes from.

### 4. Close Tharga.Mcp Requests.md entry [x]
- Marked Done (2026-04-20) with a summary and explicit cross-reference to the Platform.Mcp counterpart that still needs to ship for consumers to be unblocked.

### 5. File Platform.Mcp Requests.md entry + Follow-ups [x]
- New entry under `## Tharga.Platform`: *"HttpContextMcpContextAccessor should compute McpScope from claims instead of hardcoding User"* (Priority: High, Pending). Includes the proposed code block and explicit "consumers need both sides" note.
- Follow-ups added at the top of Requests.md for Quilt4Net.Server + PlutusWave to upgrade both packages together.

### 6. Close feature [ ]
- Archive + delete plan/
- Final commit

## Commit milestones

- After step 2: `fix: dispatcher scope filter is now a hierarchy (System sees all)`
- After step 3: `docs: README clarifies scope hierarchy semantics`
- After step 5: `chore: close scope-hierarchy request; file Platform counterpart`
- After step 6: `feat: scope-hierarchy complete`

## Progress log

_(updated during implementation)_

## Last session

_(updated at end of session)_
