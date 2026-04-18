namespace Tharga.Mcp;

/// <summary>Configuration for the Tharga MCP endpoint group.</summary>
public sealed class ThargaMcpOptions
{
    /// <summary>
    /// Base path for the three-level endpoint group. Defaults to <c>/mcp</c>, producing <c>/mcp/me</c>, <c>/mcp/team</c>, and <c>/mcp/system</c>.
    /// </summary>
    public string EndpointBasePath { get; set; } = "/mcp";

    /// <summary>
    /// When true, endpoints require an authenticated caller. Has no effect in Phase 0 — the Platform bridge (<c>Tharga.Platform.Mcp</c>) wires the actual authentication policy.
    /// </summary>
    public bool RequireAuth { get; set; } = true;
}
