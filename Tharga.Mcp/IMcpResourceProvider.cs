namespace Tharga.Mcp;

/// <summary>
/// Provides read-only resources for a single <see cref="McpScope"/>. Implementations are registered via
/// <c>IThargaMcpBuilder.AddResourceProvider&lt;T&gt;</c> and resolved per request from DI.
/// </summary>
public interface IMcpResourceProvider : IMcpProvider
{
    /// <summary>Lists resources visible to the caller.</summary>
    Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken);

    /// <summary>Reads a specific resource by its URI. Must return content for URIs advertised by <see cref="ListResourcesAsync"/>.</summary>
    Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken);
}
