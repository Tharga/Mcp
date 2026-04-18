namespace Tharga.Mcp.Internal;

internal sealed record McpProviderRegistration
{
    public required Type ImplementationType { get; init; }
}
