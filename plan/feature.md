# Feature: Expose session mode on `ThargaMcpOptions`

## Goal

Give a host a supported way to choose how the MCP HTTP transport tracks state between
requests. Today `ThargaMcpOptions` offers `EndpointBasePath`, `RequireAuth` and
`AuthenticationSchemes` only, so a host running `Tharga.Mcp` **cannot** opt out of the SDK's
stateless default — even though the SDK documents that setting as the supported escape hatch
for clients that still rely on the `initialize` handshake.

## Origin

`$DOC_ROOT/Tharga/Requests.md` → `## Tharga.Team — MCP` → *"`Tharga.Mcp` 1.0.1 carries an MCP
SDK **major** with a protocol behaviour change, unannounced"*, **ask 2**. Filed by PlutusWave
2026-08-01, priority High. Ask 1 (say it in the release notes) shipped with 1.1.0; ask 2 is the
substantive half and is still open.

The reported symptom: SDK 2.0.0 defaulted the transport to stateless per protocol revision
`2026-07-28` (SEP-2567), which removed `Mcp-Session-Id`. Every MCP client holding a session
negotiated against 1.4.1 then failed on its next call with *"Bad Request: The Mcp-Session-Id
header is not supported in stateless mode"*. PlutusWave lost every tool on both team endpoints
at once when 0.1.27 reached production, and had no option to turn it back on.

## Scope

**In scope**

- A Tharga-owned `McpSessionMode` enum with three values, mapped to the SDK's
  `HttpServerSessionMode`.
- `ThargaMcpOptions.SessionMode`, defaulting to `Stateless` — the SDK's own default, so the
  new option changes nothing for a host that does not set it.
- Passing the configured value through to `HttpServerTransportOptions` in `AddThargaMcp`.
- XML documentation stating what each mode costs, since choosing `Stateful` disables nothing
  locally but changes what modern clients get.
- README and `docs/` coverage.
- The mandatory up-front NuGet upgrade (`xunit.v3` and `xunit.runner.visualstudio` → 4.0.0).

**Out of scope**

- `EnableLegacySse`, `IdleTimeout`, `EventStreamStore` and the rest of
  `HttpServerTransportOptions`. No consumer has asked, and exposing the whole surface would
  make `ThargaMcpOptions` a second copy of the SDK's options class.
**Added to scope 2026-08-16 — `IMcpContext.IsDeveloper` removed.**

Originally out of scope as a separate ask. The user asked what the member did, and the answer
("nothing — the package never reads it") led them to decide it should go. Removed outright
rather than deprecated.

- **This makes the release 2.0.0, not 1.2.0.** Removing a member from a public interface is a
  compile break for every implementer.
- **`Tharga.Team.Mcp` is an implementer and will break.** Verified against `Tharga/Team`
  `origin/master`, not assumed: `TeamMcpContext.cs:37` computes it from the configurable
  `developerRole`, and `TeamSystemResourceProvider.cs:63` and `:135` gate the System API Keys
  and Tenant Roles resources on it. Those two gates need a replacement — this is a behaviour
  change there, not just a deleted property.
- **The filed request's claim that Tharga.Team no longer depends on it is stale**, and the
  record is corrected at close-out along with a new request filed against Tharga.Team.
- `UserId` and `TeamId` stay. The foundation does not read them either, but they are identity
  *data* the context exists to carry to providers, not a duplicated authorization verdict.

## Decisions

**An enum, not the `bool Stateless` the request literally asked for.** The request was written
against SDK 2.0.0. At 2.2.0 the SDK replaced the flag with `HttpServerSessionMode` and demoted
`Stateless` to a lossy convenience proxy: `StatefulForInitializeClients` reads back as `false`,
so a bool cannot express the mode a legacy-client host most likely wants. Shipping the bool
would mean either a breaking change later or a second property carrying the SDK's own
"last assignment wins" trap. The enum is a superset of the ask. Confirmed with the user
2026-08-15.

**Tharga-owned enum, not the SDK's.** This repo already owns its contract types and maps them
to the SDK in `McpTypeMappers` (`McpScope`, `McpToolDescriptor`, `McpContent`). Re-exporting
`HttpServerSessionMode` would put an SDK type in our public API, making an SDK enum change our
breaking change — which is the same class of problem this very request was filed about.

**The value is read at options-resolution time, not at `AddThargaMcp` call time.** `AddThargaMcp`
calls `WithHttpTransport` *before* it invokes the host's `configure(builder)` callback, so
reading `options.SessionMode` eagerly would always see the default and silently discard whatever
the host set. The configure lambda closes over the singleton `ThargaMcpOptions` instance
instead, so the value is read after the callback has run. This is the non-obvious part of the
change and gets a test of its own.

## Acceptance criteria

- [ ] `McpSessionMode` is public, has `Stateless` / `Stateful` / `StatefulForInitializeClients`,
      and every member carries XML documentation naming what it costs.
- [ ] `ThargaMcpOptions.SessionMode` defaults to `McpSessionMode.Stateless`.
- [ ] A host setting `mcp.Options.SessionMode` inside the `AddThargaMcp` callback sees that
      value on the resolved `HttpServerTransportOptions` — proven by test, because the
      registration order makes the naive implementation fail silently.
- [ ] Every `McpSessionMode` member maps to the matching `HttpServerSessionMode` member —
      proven by a test that enumerates the enum rather than listing cases, so a new SDK member
      cannot be added without the test noticing.
- [ ] A host that sets nothing still gets stateless, unchanged from 1.1.0.
- [ ] `xunit.v3` and `xunit.runner.visualstudio` are on 4.0.0 with the suite green.
- [ ] Build has 0 warnings and the full test suite passes.
- [ ] README and `docs/` document the option, including that `Stateful` refuses
      `2026-07-28`+ requests with `-32022 UnsupportedProtocolVersion` and forces a downgrade.
- [ ] `MAJOR_MINOR` moved `1.1` → `2.0` — additive on its own, but the `IsDeveloper` removal
      added to scope is a public-interface break, so it ships as a major.
- [ ] `IMcpContext.IsDeveloper` is gone, with no member left naming a host-configurable role,
      and `docs/articles/providers.md` records the removal and what to gate on instead.

## Done condition

`Tharga.Mcp` 1.2.0 lets a host write `mcp.Options.SessionMode = McpSessionMode.Stateful` (or
`StatefulForInitializeClients`) and get that transport behaviour, with the request in
`Requests.md` updated to Done citing the type, the member and the tests.
