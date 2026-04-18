namespace Tharga.Mcp;

/// <summary>
/// Per-request accessor for the active <see cref="IMcpContext"/>. Populated by the transport pipeline before a provider is invoked.
/// In Phase 0 the default implementation is empty (AsyncLocal-backed). <c>Tharga.Platform.Mcp</c> replaces it with a Platform-backed implementation.
/// </summary>
public interface IMcpContextAccessor
{
    /// <summary>The current call's context, or <c>null</c> when outside an MCP request.</summary>
    IMcpContext Current { get; set; }
}
