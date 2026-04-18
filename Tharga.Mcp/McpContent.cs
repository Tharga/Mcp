namespace Tharga.Mcp;

/// <summary>A single content part returned from a tool call. Phase 0 supports text; richer kinds (image, audio) arrive later.</summary>
public sealed record McpContent
{
    /// <summary>Content kind. Currently <c>text</c>.</summary>
    public string Type { get; init; } = "text";

    /// <summary>Text payload when <see cref="Type"/> is <c>text</c>.</summary>
    public string Text { get; init; }
}
