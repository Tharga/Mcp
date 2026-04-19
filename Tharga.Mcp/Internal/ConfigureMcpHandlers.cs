using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Tharga.Mcp.Internal;

internal sealed class ConfigureMcpHandlers(McpProviderDispatcher dispatcher) : IConfigureOptions<McpServerOptions>
{
    public void Configure(McpServerOptions options)
    {
        var handlers = options.Handlers;
        handlers.ListToolsHandler ??= dispatcher.ListToolsAsync;
        handlers.CallToolHandler ??= dispatcher.CallToolAsync;
        handlers.ListResourcesHandler ??= dispatcher.ListResourcesAsync;
        handlers.ReadResourceHandler ??= dispatcher.ReadResourceAsync;
    }
}
