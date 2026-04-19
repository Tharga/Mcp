using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Tharga.Mcp.Internal;

internal static class McpTypeMappers
{
    private static readonly JsonElement _defaultInputSchema = JsonDocument.Parse("""{"type":"object"}""").RootElement;

    public static Tool ToSdkTool(McpToolDescriptor descriptor) => new()
    {
        Name = descriptor.Name,
        Description = descriptor.Description,
        InputSchema = descriptor.InputSchema ?? _defaultInputSchema,
    };

    public static Resource ToSdkResource(McpResourceDescriptor descriptor) => new()
    {
        Name = descriptor.Name,
        Uri = descriptor.Uri,
        Description = descriptor.Description,
        MimeType = descriptor.MimeType,
    };

    public static CallToolResult ToSdkCallToolResult(McpToolResult result) => new()
    {
        Content = result.Content.Select(ToSdkContentBlock).ToList<ContentBlock>(),
        IsError = result.IsError ? true : null,
    };

    public static ReadResourceResult ToSdkReadResourceResult(McpResourceContent content) => new()
    {
        Contents = [ToSdkResourceContents(content)],
    };

    public static JsonElement ArgumentsToJsonElement(IDictionary<string, JsonElement> arguments)
    {
        if (arguments == null || arguments.Count == 0)
        {
            return JsonDocument.Parse("{}").RootElement;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var kvp in arguments)
            {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        stream.Position = 0;
        return JsonDocument.Parse(stream).RootElement;
    }

    private static TextContentBlock ToSdkContentBlock(McpContent content)
        => new() { Text = content.Text ?? string.Empty };

    private static ResourceContents ToSdkResourceContents(McpResourceContent content)
    {
        if (content.Blob != null)
        {
            return BlobResourceContents.FromBytes(content.Blob, content.Uri, content.MimeType);
        }

        return new TextResourceContents
        {
            Uri = content.Uri,
            Text = content.Text ?? string.Empty,
            MimeType = content.MimeType,
        };
    }
}
