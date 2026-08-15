namespace Tharga.Mcp;

/// <summary>
/// How the MCP HTTP transport tracks state between requests.
/// Set through <see cref="ThargaMcpOptions.SessionMode"/>.
/// </summary>
/// <remarks>
/// Protocol revision <c>2026-07-28</c> removed <c>Mcp-Session-Id</c> (SEP-2567) and the <c>initialize</c>
/// handshake (SEP-2575), so a client on that revision or later can only ever be served statelessly.
/// This enumeration is how a host chooses what happens to clients that still expect a session.
/// </remarks>
public enum McpSessionMode
{
    /// <summary>
    /// Never track state between requests. The default, and what the protocol requires from
    /// <c>2026-07-28</c> onward.
    /// </summary>
    /// <remarks>
    /// No session id is minted or echoed, and the GET, DELETE and <c>/sse</c> endpoints answer
    /// <c>405 Method Not Allowed</c>. The server cannot send unsolicited messages or make requests of the
    /// client, so sampling, elicitation and roots are unavailable. In exchange the endpoint needs no
    /// session affinity and can sit behind more than one instance.
    /// </remarks>
    Stateless,

    /// <summary>
    /// Track a long-lived session for every client, which requires session affinity.
    /// </summary>
    /// <remarks>
    /// Choose this only to keep pre-<c>2026-07-28</c> clients working. A client that <i>does</i> declare
    /// <c>2026-07-28</c> or later is refused with <c>-32022 UnsupportedProtocolVersion</c> and has to
    /// downgrade to the <c>initialize</c> handshake, so a modern client pays for a legacy one.
    /// <see cref="StatefulForInitializeClients"/> avoids that trade.
    /// </remarks>
    Stateful,

    /// <summary>
    /// Track a session for clients that use the <c>initialize</c> handshake, and serve
    /// <c>2026-07-28</c> and later clients statelessly on the same endpoint.
    /// </summary>
    /// <remarks>
    /// The migration mode: legacy clients keep their session id and the GET and DELETE endpoints, while
    /// modern clients get exactly what <see cref="Stateless"/> gives them, with no downgrade forced on
    /// either. Session-only features remain unavailable to the stateless half.
    /// </remarks>
    StatefulForInitializeClients,
}
