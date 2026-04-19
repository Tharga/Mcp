using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Tharga.Mcp.Internal;

internal sealed class McpProviderDispatcher
{
    public async ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> request, CancellationToken cancellationToken)
    {
        var (context, providers) = Resolve<IMcpToolProvider>(request);
        var result = new ListToolsResult();
        foreach (var provider in providers)
        {
            var descriptors = await provider.ListToolsAsync(context, cancellationToken).ConfigureAwait(false);
            foreach (var descriptor in descriptors)
            {
                result.Tools.Add(McpTypeMappers.ToSdkTool(descriptor));
            }
        }
        return result;
    }

    public async ValueTask<CallToolResult> CallToolAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken)
    {
        var toolName = request.Params?.Name
            ?? throw new ArgumentException("tools/call requires a tool name.", nameof(request));
        var (context, providers) = Resolve<IMcpToolProvider>(request);

        foreach (var provider in providers)
        {
            var descriptors = await provider.ListToolsAsync(context, cancellationToken).ConfigureAwait(false);
            if (!descriptors.Any(d => d.Name == toolName)) continue;

            var arguments = request.Params?.Arguments != null
                ? McpTypeMappers.ArgumentsToJsonElement(request.Params.Arguments)
                : McpTypeMappers.ArgumentsToJsonElement(new Dictionary<string, System.Text.Json.JsonElement>());

            var result = await provider.CallToolAsync(toolName, arguments, context, cancellationToken).ConfigureAwait(false);
            return McpTypeMappers.ToSdkCallToolResult(result);
        }

        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = $"Unknown tool: {toolName}" }],
        };
    }

    public async ValueTask<ListResourcesResult> ListResourcesAsync(RequestContext<ListResourcesRequestParams> request, CancellationToken cancellationToken)
    {
        var (context, providers) = Resolve<IMcpResourceProvider>(request);
        var result = new ListResourcesResult();
        foreach (var provider in providers)
        {
            var descriptors = await provider.ListResourcesAsync(context, cancellationToken).ConfigureAwait(false);
            foreach (var descriptor in descriptors)
            {
                result.Resources.Add(McpTypeMappers.ToSdkResource(descriptor));
            }
        }
        return result;
    }

    public async ValueTask<ReadResourceResult> ReadResourceAsync(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken)
    {
        var uri = request.Params?.Uri
            ?? throw new ArgumentException("resources/read requires a uri.", nameof(request));
        var (context, providers) = Resolve<IMcpResourceProvider>(request);

        foreach (var provider in providers)
        {
            var descriptors = await provider.ListResourcesAsync(context, cancellationToken).ConfigureAwait(false);
            if (!descriptors.Any(d => d.Uri == uri)) continue;

            var content = await provider.ReadResourceAsync(uri, context, cancellationToken).ConfigureAwait(false);
            return McpTypeMappers.ToSdkReadResourceResult(content);
        }

        throw new InvalidOperationException($"Unknown resource uri: {uri}");
    }

    private static (IMcpContext Context, IEnumerable<T> Providers) Resolve<T>(MessageContext request) where T : class, IMcpProvider
    {
        var services = request.Services
            ?? throw new InvalidOperationException("MCP request is missing a per-request IServiceProvider.");

        var accessor = services.GetService<IMcpContextAccessor>();
        var current = accessor?.Current;
        var context = current ?? FallbackContext.Default;

        var allProviders = services.GetServices<T>();
        var filtered = current is null
            ? allProviders
            : allProviders.Where(p => p.Scope == current.Scope);

        return (context, filtered);
    }

    private sealed class FallbackContext : IMcpContext
    {
        public static readonly FallbackContext Default = new();
        public string UserId => null;
        public string TeamId => null;
        public bool IsDeveloper => true;
        public McpScope Scope => McpScope.System;
    }
}
