# Plan: RequireAuth accepts the credential MCP callers actually use

Branch `fix/auth-schemes`, from `master`. Issue [#18](https://github.com/Tharga/Mcp/issues/18).

Version stays at `MAJOR_MINOR: '1.0'` (decided with the user) — the change is additive, so CI
ships it as `1.0.1`.

## Steps

- [x] 1. Upgrade ModelContextProtocol + ModelContextProtocol.AspNetCore 1.4.1 → 2.0.0, resolve any
      breakage, verify build + full test suite. Done first so upgrade breakage is never tangled
      with the fix.
      *Done — no library source change needed. 2.0.0 makes `HttpServerTransportOptions.Stateless`
      default to true, so the server issues no `Mcp-Session-Id`; the raw test client demanded that
      header and 8 bridge tests failed. It now takes a session only when one is offered. 32/32 pass.
      Consequence for consumers: the endpoint is stateless by default from this release — see
      step 4.*
- [x] 2. `ThargaMcpOptions.AuthenticationSchemes` and the policy built from it in `UseThargaMcp()`.
      *Done. The policy always names the contributed schemes — an empty list adds none, which is
      exactly the previous default-scheme behavior, so no host changes on upgrade.*
- [x] 3. Tests over the endpoint's authorization policy metadata.
      *Done — 5 tests. Asserted on policy metadata rather than over HTTP: the MCP endpoint
      negotiates content before a credential matters, so a status code would describe the request
      body more than the policy. 32/32 pass.*
- [x] 4. Documentation — `README.md`, `docs/index.md`, `docs/articles/authorization.md`,
      `docs/articles/getting-started.md`.
      *Done, plus `docs/articles/index.md`. `authorization.md` gained a "Which credential is
      accepted" section — the 302-to-login symptom, naming one or several schemes, and that
      stacking a second policy narrows rather than widens. Also documented the 2.0.0 stateless
      endpoint in `getting-started.md`. `docfx docs/docfx.json` builds with 0 warnings.*
- [~] 5. Record the `Tharga.Team.Mcp` follow-up (its `AddTeam()` contributes the API-key scheme).
- [ ] 6. Push the branch for the user to test. No PR until they confirm.

## Close-out (only when the user says the feature is done)

- [ ] Re-run `dotnet outdated`; apply anything newly published, in this PR.
- [ ] Archive `plan/feature.md` to the Plan directory `done/`.
- [ ] `git rm -r plan`, final commit `fix: require-auth schemes complete`, push, open the PR.

## Notes

Steps 2 and 3 arrived already written in the working tree at the start of this session — carried
onto this branch rather than redone. They are verified against 2.0.0 as part of step 1.
