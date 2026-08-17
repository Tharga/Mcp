# Scopes

Tharga.Mcp models access as a three-level **hierarchy**:

| Scope | Numeric value | What the caller is |
|---|---|---|
| `User` | 0 | Authenticated user, no team / system privileges |
| `Team` | 1 | Authenticated user with an active team |
| `System` | 2 | Developer-level / system API key — infrastructure access |

The dispatcher filters registered providers using **`provider.Scope <= caller.Scope`** — a System caller sees providers from every scope; a Team caller sees User + Team; a User caller sees only User.

```
caller = System   → User ✓   Team ✓   System ✓
caller = Team     → User ✓   Team ✓   System ✗
caller = User     → User ✓   Team ✗   System ✗
caller = (null)   → User ✓   Team ✓   System ✓  (no auth wired — see "anonymous" below)
```

## Declaring a provider's scope

Every `IMcpProvider` declares one scope:

```csharp
public sealed class MongoDbToolProvider : IMcpToolProvider
{
    public McpScope Scope => McpScope.System;
    // ...
}
```

A provider that conceptually spans multiple scopes ships as multiple classes — one per scope — registered together via the package's `AddX()` extension. This forces clean separation: the System-scope class can't accidentally surface to a User caller.

## Where the caller's scope comes from

The dispatcher reads `IMcpContextAccessor.Current.Scope`. Two paths populate it:

- **`Tharga.Team.Mcp`** — replaces `IMcpContextAccessor` with one that derives the context from `HttpContext.User` per request:
  - `Developer` role or `IsSystemKey=true` claim → `System`
  - non-empty `TeamKey` claim, or a team named on the call → `Team`
  - otherwise → `User`
- **Custom** — you can register your own `IMcpContextAccessor` implementation if you have a different identity model. The dispatcher only requires that `Current` be populated before it runs.

## Anonymous behavior

When `IMcpContextAccessor.Current` is `null` — the default for a `Tharga.Mcp` host with no bridge package — the dispatcher **skips scope filtering entirely**: every registered provider is visible. Two reasons:

1. **Phase 0 ergonomics** — a sample host without auth shouldn't return an empty `tools/list`. The "show everything" fallback keeps the demo path usable.
2. **No false sense of security** — relying on a hierarchy filter alone for auth would be wrong anyway. Auth lives one layer up: `RequireAuth = true` + the `Authorize` attribute control *whether* a request reaches the dispatcher; the hierarchy decides *what* an authorized caller sees.

In a host with `Tharga.Team.Mcp` wired up, `Current` is populated on every authenticated request, so the null path is not reached. Note the corollary: until you wire a bridge or your own accessor, the `McpScope` on each provider is decorative — declaring a provider `System` does not hide it from anyone.

## Why hierarchy and not strict equality

An earlier version of the dispatcher filtered with `provider.Scope == caller.Scope`. That made a System caller see only System providers — hiding everything a Developer-level caller should obviously be allowed to see, including all User and Team data. The hierarchy filter restores the principle of least surprise: higher privilege includes lower privilege.

## Tests in the box

`Tharga.Mcp.Tests/Bridge/ProviderBridgeTests` locks in the hierarchy with three tests:

- `User_scope_caller_sees_only_user_scope_providers`
- `Team_scope_caller_sees_user_and_team_providers_but_not_system`
- `System_scope_caller_sees_providers_from_all_scopes`

Each test uses a middleware that sets `accessor.Current` to a fake context — the same seam `Tharga.Team.Mcp` fills in production.
