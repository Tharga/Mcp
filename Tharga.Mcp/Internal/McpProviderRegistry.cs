namespace Tharga.Mcp.Internal;

internal sealed class McpProviderRegistry
{
    private readonly List<McpProviderRegistration> _resourceProviders = [];
    private readonly List<McpProviderRegistration> _toolProviders = [];

    public IReadOnlyList<McpProviderRegistration> ResourceProviders => _resourceProviders;
    public IReadOnlyList<McpProviderRegistration> ToolProviders => _toolProviders;

    public bool AddResourceProvider(Type implementationType)
    {
        if (_resourceProviders.Any(x => x.ImplementationType == implementationType)) return false;
        _resourceProviders.Add(new McpProviderRegistration { ImplementationType = implementationType });
        return true;
    }

    public bool AddToolProvider(Type implementationType)
    {
        if (_toolProviders.Any(x => x.ImplementationType == implementationType)) return false;
        _toolProviders.Add(new McpProviderRegistration { ImplementationType = implementationType });
        return true;
    }
}
