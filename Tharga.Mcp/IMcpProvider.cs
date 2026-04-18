namespace Tharga.Mcp;

/// <summary>
/// Base contract shared by <see cref="IMcpResourceProvider"/> and <see cref="IMcpToolProvider"/>.
/// A provider declares the single scope at which its contents are exposed.
/// </summary>
public interface IMcpProvider
{
    /// <summary>The scope this provider belongs to. Only the matching endpoint will see its contents.</summary>
    McpScope Scope { get; }
}
