using System.Text.Json;

namespace Tharga.Mcp;

/// <summary>
/// Provides callable tools for a single <see cref="McpScope"/>. Implementations are registered via
/// <c>IThargaMcpBuilder.AddToolProvider&lt;T&gt;</c> and resolved per request from DI.
/// </summary>
public interface IMcpToolProvider : IMcpProvider
{
    /// <summary>Lists tools visible to the caller.</summary>
    Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken);

    /// <summary>Invokes a tool by name with the supplied arguments.</summary>
    /// <param name="toolName">Name of the tool as advertised by <see cref="ListToolsAsync"/>.</param>
    /// <param name="arguments">Raw JSON arguments from the client, already validated against <see cref="McpToolDescriptor.InputSchema"/>.</param>
    /// <param name="context">The per-call context.</param>
    /// <param name="cancellationToken">Cancellation signal from the transport.</param>
    Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken);
}
