using System.Text.Json;

namespace Tharga.Mcp;

/// <summary>Metadata for a tool surfaced to MCP clients via <see cref="IMcpToolProvider.ListToolsAsync"/>.</summary>
public sealed record McpToolDescriptor
{
    /// <summary>Unique tool name within its provider (e.g. <c>mongodb.touch</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Optional human-readable description of what the tool does.</summary>
    public string? Description { get; init; }

    /// <summary>JSON Schema describing the tool's arguments. When null, the tool takes no arguments.</summary>
    public JsonElement? InputSchema { get; init; }
}
