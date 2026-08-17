# Session mode

`ThargaMcpOptions.SessionMode` decides how the HTTP transport tracks state between requests.

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.Options.SessionMode = McpSessionMode.StatefulForInitializeClients;
});
```

The default is `McpSessionMode.Stateless`, and most hosts should leave it alone.

## Why this option exists

Protocol revision `2026-07-28` removed `Mcp-Session-Id` (SEP-2567) and the `initialize` handshake
(SEP-2575). ModelContextProtocol 2.0.0 implemented that by defaulting the HTTP transport to
stateless, and `Tharga.Mcp` picked the change up transitively.

For clients that had negotiated a session against an earlier revision, the next call after that
upgrade fails:

```
Bad Request: The Mcp-Session-Id header is not supported in stateless mode
```

The message is accurate but misleading — it reads as a misconfigured server rather than as the
protocol revision changing underneath the client. Until this option existed there was no way to
turn it back on through `Tharga.Mcp`, so a host in that position had no route forward but to pin an
older package.

## The three modes

| Mode | Legacy clients | `2026-07-28`+ clients | Needs session affinity |
|---|---|---|---|
| `Stateless` *(default)* | Rejected | Served | No |
| `Stateful` | Full session | **Forced to downgrade** | Yes |
| `StatefulForInitializeClients` | Full session | Served statelessly | Yes |

### `Stateless`

No session id is minted or echoed. The GET, DELETE and `/sse` endpoints answer
`405 Method Not Allowed`. The server cannot send unsolicited messages or make requests of the
client, so **sampling, elicitation and roots are unavailable** — use
[MRTR](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/mrtr/mrtr.html) instead.

In exchange the endpoint needs no session affinity, so it can sit behind more than one instance.
That is why it is the right default and worth returning to once your clients have moved.

### `Stateful`

Every client gets a long-lived session, which requires session affinity.

The cost is easy to miss: a client that declares `2026-07-28` or later is refused with
`-32022 UnsupportedProtocolVersion` and has to downgrade to the `initialize` handshake. **A modern
client pays for a legacy one.** If both kinds call the same endpoint, prefer the hybrid below.

### `StatefulForInitializeClients`

The migration mode. Clients on `2025-11-25` or earlier get a full session with an `Mcp-Session-Id`
and keep using the GET and DELETE endpoints; clients on `2026-07-28` or later are served per
request with no session minted, exactly as in `Stateless`. Neither is forced to downgrade.

This lets an application adopt the new revision progressively instead of waiting for every client
to migrate. Session-only features stay unavailable to the stateless half.

## When the value is read

`SessionMode` is read when the transport options are resolved, not when `AddThargaMcp` returns, so
setting it anywhere inside the registration callback works:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddTeam();
    mcp.Options.SessionMode = McpSessionMode.Stateful;   // order does not matter
});
```

## Choosing

- **New deployment, or all clients current** — leave it at `Stateless`.
- **Existing clients broke on the upgrade to 2.x** — `StatefulForInitializeClients`.
- **Every client is legacy and you want the simplest thing** — `Stateful`, accepting that it needs
  session affinity and that any modern client will be downgraded.

Treat anything other than `Stateless` as temporary. Session affinity is a real constraint on how the
endpoint can be hosted, and the protocol has moved.
