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
}
