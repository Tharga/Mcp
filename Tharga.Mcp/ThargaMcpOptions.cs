namespace Tharga.Mcp;

/// <summary>Configuration for the Tharga MCP endpoint group.</summary>
public sealed class ThargaMcpOptions
{
    /// <summary>
    /// Base path for the three-level endpoint group. Defaults to <c>/mcp</c>, producing <c>/mcp/me</c>, <c>/mcp/team</c>, and <c>/mcp/system</c>.
    /// </summary>
    public string EndpointBasePath { get; set; } = "/mcp";

    /// <summary>
    /// When true (the default), endpoints require an authenticated caller.
    /// </summary>
    /// <remarks>
    /// Authentication is attempted against <see cref="AuthenticationSchemes"/> when any have been
    /// contributed, and against the application's default scheme otherwise.
    /// </remarks>
    public bool RequireAuth { get; set; } = true;

    /// <summary>
    /// Authentication schemes the MCP endpoint accepts. Empty means the application's default scheme.
    /// </summary>
    /// <remarks>
    /// Bridge packages add the schemes their callers actually use — <c>Tharga.Team.Mcp</c> adds the
    /// API-key scheme — so a host gets a working endpoint without knowing about schemes at all.
    /// <para>
    /// Without this, <see cref="RequireAuth"/> could say <i>that</i> authentication is required but not
    /// <i>how</i>: a bare <c>RequireAuthorization()</c> resolves to the default scheme, which in any host
    /// with interactive sign-in is OIDC. MCP callers are agents presenting an API key, so the single
    /// credential the endpoint exists to serve was the one it rejected — answering with a 302 to a login
    /// page (Tharga/Mcp#18).
    /// </para>
    /// <para>
    /// A host may add its own — a cookie scheme, say — so a signed-in user can explore the endpoint
    /// alongside agents.
    /// </para>
    /// </remarks>
    public IList<string> AuthenticationSchemes { get; } = [];

    /// <summary>
    /// How the HTTP transport tracks state between requests. Defaults to
    /// <see cref="McpSessionMode.Stateless"/>, which is what the protocol requires from revision
    /// <c>2026-07-28</c> onward.
    /// </summary>
    /// <remarks>
    /// Set this only to keep clients working that were written against an older revision. Those clients
    /// negotiated an <c>Mcp-Session-Id</c>, which SEP-2567 removed from the protocol; against a stateless
    /// server their next call fails with <i>"The Mcp-Session-Id header is not supported in stateless
    /// mode"</i>, which reads as a server misconfiguration rather than as a protocol revision changing
    /// underneath them.
    /// <para>
    /// Prefer <see cref="McpSessionMode.StatefulForInitializeClients"/> over
    /// <see cref="McpSessionMode.Stateful"/> when both old and new clients call the same endpoint —
    /// <see cref="McpSessionMode.Stateful"/> forces a downgrade on every modern client to accommodate the
    /// legacy ones.
    /// </para>
    /// <para>
    /// Stateless is the better default and worth returning to: it needs no session affinity, so the
    /// endpoint can sit behind more than one instance.
    /// </para>
    /// </remarks>
    public McpSessionMode SessionMode { get; set; } = McpSessionMode.Stateless;
}
