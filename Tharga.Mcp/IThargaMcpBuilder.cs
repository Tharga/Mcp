using Microsoft.Extensions.DependencyInjection;

namespace Tharga.Mcp;

/// <summary>
/// Fluent builder passed to the <c>AddThargaMcp</c> callback. Provider packages expose extension methods on
/// this interface (e.g. <c>AddMongoDB()</c>) so that registration composes inside the callback scope without
/// colliding with existing <c>IServiceCollection</c> extensions.
/// </summary>
public interface IThargaMcpBuilder
{
    /// <summary>The underlying service collection, for advanced composition scenarios.</summary>
    IServiceCollection Services { get; }

    /// <summary>The configured options. Mutate directly in the callback; consumed when endpoints are mapped.</summary>
    ThargaMcpOptions Options { get; }

    /// <summary>Registers a resource provider type. The provider's declared <see cref="IMcpProvider.Scope"/> determines which endpoint exposes it.</summary>
    IThargaMcpBuilder AddResourceProvider<T>() where T : class, IMcpResourceProvider;

    /// <summary>Registers a tool provider type. The provider's declared <see cref="IMcpProvider.Scope"/> determines which endpoint exposes it.</summary>
    IThargaMcpBuilder AddToolProvider<T>() where T : class, IMcpToolProvider;
}
