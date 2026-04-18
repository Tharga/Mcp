namespace Tharga.Mcp;

/// <summary>
/// The access level at which an MCP provider surfaces its tools and resources.
/// Maps one-to-one to an endpoint exposed by <c>MapMcp</c>.
/// </summary>
public enum McpScope
{
    /// <summary>Personal data for the authenticated user only. Endpoint: <c>/mcp/me</c>.</summary>
    User,

    /// <summary>Data scoped to the user's active team. Endpoint: <c>/mcp/team</c>.</summary>
    Team,

    /// <summary>Application-wide infrastructure and cross-team data. Endpoint: <c>/mcp/system</c>. Typically requires the Developer role.</summary>
    System,
}
