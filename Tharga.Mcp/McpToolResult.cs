namespace Tharga.Mcp;

/// <summary>The outcome of a tool invocation.</summary>
public sealed record McpToolResult
{
    /// <summary>One or more content parts to return to the client.</summary>
    public required IReadOnlyList<McpContent> Content { get; init; }

    /// <summary>True when the content represents an error rather than a successful result.</summary>
    public bool IsError { get; init; }
}
