namespace Tharga.Mcp;

/// <summary>The payload returned from <see cref="IMcpResourceProvider.ReadResourceAsync"/>.</summary>
public sealed record McpResourceContent
{
    /// <summary>The URI this content was read from.</summary>
    public required string Uri { get; init; }

    /// <summary>Textual content (mutually exclusive with <see cref="Blob"/>).</summary>
    public string Text { get; init; }

    /// <summary>Binary content as base64-encoded bytes (mutually exclusive with <see cref="Text"/>).</summary>
    public byte[] Blob { get; init; }

    /// <summary>Optional MIME type of the content.</summary>
    public string MimeType { get; init; }
}
