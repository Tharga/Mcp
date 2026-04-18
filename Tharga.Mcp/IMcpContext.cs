namespace Tharga.Mcp;

/// <summary>
/// Per-call execution context surfaced to <see cref="IMcpResourceProvider"/> and <see cref="IMcpToolProvider"/> implementations.
/// The default implementation shipped with Tharga.Mcp is anonymous. <c>Tharga.Platform.Mcp</c> replaces it with a Platform-backed context.
/// </summary>
public interface IMcpContext
{
    /// <summary>The authenticated user, or <c>null</c> when the request is anonymous.</summary>
    string UserId { get; }

    /// <summary>The active team for this call, or <c>null</c> when the scope is not team-bound.</summary>
    string TeamId { get; }

    /// <summary>True when the caller has the Developer role. Required for the <see cref="McpScope.System"/> endpoint.</summary>
    bool IsDeveloper { get; }

    /// <summary>The endpoint scope that served this call.</summary>
    McpScope Scope { get; }
}
