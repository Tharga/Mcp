using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Tharga.Mcp.Internal;

internal sealed class ThargaMcpBuilder(IServiceCollection services, ThargaMcpOptions options, McpProviderRegistry registry) : IThargaMcpBuilder
{
    public IServiceCollection Services { get; } = services;
    public ThargaMcpOptions Options { get; } = options;

    public IThargaMcpBuilder AddResourceProvider<T>() where T : class, IMcpResourceProvider
    {
        if (!registry.AddResourceProvider(typeof(T))) return this;
        Services.TryAddTransient<T>();
        Services.AddTransient<IMcpResourceProvider>(sp => sp.GetRequiredService<T>());
        return this;
    }

    public IThargaMcpBuilder AddToolProvider<T>() where T : class, IMcpToolProvider
    {
        if (!registry.AddToolProvider(typeof(T))) return this;
        Services.TryAddTransient<T>();
        Services.AddTransient<IMcpToolProvider>(sp => sp.GetRequiredService<T>());
        return this;
    }
}
