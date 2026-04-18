namespace Tharga.Mcp;

/// <summary>Metadata for a resource surfaced to MCP clients via <see cref="IMcpResourceProvider.ListResourcesAsync"/>.</summary>
public sealed record McpResourceDescriptor
{
    /// <summary>Canonical URI identifying the resource (e.g. <c>mongodb://collections/users</c>).</summary>
    public required string Uri { get; init; }

    /// <summary>Human-readable name shown to clients.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description surfaced in tool/resource listings.</summary>
    public string Description { get; init; }

    /// <summary>Optional MIME type hint for the resource content (e.g. <c>application/json</c>).</summary>
    public string MimeType { get; init; }
}
