using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Tharga.Mcp;

/// <summary>Endpoint-mapping extensions for Tharga MCP.</summary>
public static class UseThargaMcpExtensions
{
    /// <summary>
    /// Maps the MCP HTTP endpoint group at <see cref="ThargaMcpOptions.EndpointBasePath"/> (default <c>/mcp</c>).
    /// The configured <c>ModelContextProtocol</c> server handles discovery and tool/resource invocation.
    /// </summary>
    /// <remarks>
    /// Phase 0 exposes a single endpoint. The three-level endpoint split (<c>/mcp/me</c>, <c>/mcp/team</c>, <c>/mcp/system</c>)
    /// is deferred — scope is carried on <see cref="IMcpContext.Scope"/> and enforced by providers. See the master plan decision 2026-04-18.
    /// </remarks>
    public static IEndpointConventionBuilder UseThargaMcp(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<ThargaMcpOptions>();
        return endpoints.MapMcp(options.EndpointBasePath);
    }

    /// <summary>
    /// Obsolete alias for <see cref="UseThargaMcp(IEndpointRouteBuilder)"/>. Kept for one release cycle to avoid breaking existing consumers at the point of upgrade.
    /// </summary>
    [Obsolete("Use UseThargaMcp() instead. MapMcp will be removed in a future version.")]
    public static IEndpointConventionBuilder MapMcp(this IEndpointRouteBuilder endpoints)
        => endpoints.UseThargaMcp();
}
