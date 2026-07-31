# Feature: RequireAuth accepts the credential MCP callers actually use

Fixes [Tharga/Mcp#18](https://github.com/Tharga/Mcp/issues/18).

## Goal

`ThargaMcpOptions.RequireAuth` must be able to express *which* schemes authenticate the MCP
endpoint, not merely *that* authentication is required.

## Background

`UseThargaMcp()` applied a bare `.RequireAuthorization()`, which resolves to the **default
authorization policy** and therefore the host's **default authentication scheme**. In a host with
interactive sign-in that scheme is OIDC, so an API-key-authenticated MCP caller was never
consulted — it was challenged and redirected to a login page.

MCP callers are agents. No user is involved, so an API key is the expected credential, and it was
the one configuration that could not work out of the box.

## Scope

In this repository:

- `ThargaMcpOptions.AuthenticationSchemes` — schemes the endpoint accepts; empty preserves today's
  behavior exactly.
- `UseThargaMcp()` builds the policy from those schemes instead of deferring to the default one.
- Tests asserting the endpoint's authorization policy metadata.
- Documentation: `README.md` and the `docs/` site, which describe the superseded behavior.
- ModelContextProtocol packages upgraded to 2.0.0 (start-of-feature dependency currency).

Out of scope — a different repository, raised as a follow-up request:

- `Tharga.Team.Mcp`'s `AddTeam()` contributing `ApiKeyConstants.SchemeName`, which is what makes
  the fix work without a host knowing anything about schemes.

## Acceptance criteria

- [ ] A host can name the schemes the MCP endpoint accepts, without replacing the built-in
      requirement or writing its own policy.
- [ ] An empty scheme list behaves exactly as before — no host is affected by upgrading.
- [ ] Naming schemes never weakens the requirement: anonymous callers are still refused.
- [ ] `RequireAuth = false` still applies no policy at all.
- [ ] `README.md` and `docs/` describe the current behavior, and say what a host must do for an
      API-key caller to be accepted.
- [ ] Solution builds and the full test suite passes on ModelContextProtocol 2.0.0.

## Done condition

The above are met, CI is green on the PR, and the follow-up for `Tharga.Team.Mcp` is recorded.
