using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tharga.Mcp;

/// <summary>Endpoint-mapping extensions for Tharga MCP.</summary>
public static class MapMcpExtensions
{
    /// <summary>
    /// Maps the MCP HTTP endpoint group at <see cref="ThargaMcpOptions.EndpointBasePath"/> (default <c>/mcp</c>).
    /// The configured <c>ModelContextProtocol</c> server handles discovery and tool/resource invocation.
    /// </summary>
    /// <remarks>
    /// Phase 0 exposes a single endpoint. The three-level endpoint split (<c>/mcp/me</c>, <c>/mcp/team</c>, <c>/mcp/system</c>)
    /// is deferred — scope is carried on <see cref="IMcpContext.Scope"/> and enforced by providers. See the master plan decision 2026-04-18.
    /// </remarks>
    public static IEndpointConventionBuilder MapMcp(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<ThargaMcpOptions>();
        return endpoints.MapMcp(options.EndpointBasePath);
    }
}
