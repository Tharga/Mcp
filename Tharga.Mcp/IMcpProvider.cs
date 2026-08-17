namespace Tharga.Mcp;

/// <summary>
/// Base contract shared by <see cref="IMcpResourceProvider"/> and <see cref="IMcpToolProvider"/>.
/// A provider declares the single scope at which its contents are exposed.
/// </summary>
public interface IMcpProvider
{
    /// <summary>
    /// The scope this provider belongs to. A caller sees it when their own
    /// <see cref="IMcpContext.Scope"/> is at or above this value — so a <see cref="McpScope.System"/>
    /// caller sees User, Team and System providers, and a <see cref="McpScope.User"/> caller sees only User.
    /// </summary>
    McpScope Scope { get; }
}
