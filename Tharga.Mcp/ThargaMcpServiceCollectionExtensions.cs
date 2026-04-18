using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tharga.Mcp.Internal;

namespace Tharga.Mcp;

/// <summary>Service-collection extensions for Tharga MCP registration.</summary>
public static class ThargaMcpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Tharga MCP foundation and invokes the bundled callback so provider packages can attach via
    /// their own extension methods on <see cref="IThargaMcpBuilder"/> (e.g. <c>mcp.AddMongoDB()</c>).
    /// Also wires up the official ModelContextProtocol server with HTTP+SSE transport.
    /// Calling this more than once merges into the existing configuration rather than throwing.
    /// </summary>
    public static IServiceCollection AddThargaMcp(this IServiceCollection services, Action<IThargaMcpBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var registry = GetOrCreateSingleton(services, () => new McpProviderRegistry());
        var options = GetOrCreateSingleton(services, () => new ThargaMcpOptions());

        services.TryAddSingleton<IMcpContextAccessor, McpContextAccessor>();

        services.AddMcpServer().WithHttpTransport();

        var builder = new ThargaMcpBuilder(services, options, registry);
        configure(builder);

        return services;
    }

    private static T GetOrCreateSingleton<T>(IServiceCollection services, Func<T> factory) where T : class
    {
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(T))?.ImplementationInstance as T;
        if (existing is not null) return existing;

        var instance = factory();
        services.AddSingleton(instance);
        return instance;
    }
}
