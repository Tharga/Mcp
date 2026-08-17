namespace Tharga.Mcp;

/// <summary>
/// Per-request accessor for the active <see cref="IMcpContext"/>, read before a provider is invoked.
/// </summary>
/// <remarks>
/// The default implementation is an empty <c>AsyncLocal</c> holder, and nothing in this package ever
/// assigns to it — so in a <c>Tharga.Mcp</c>-only host <see cref="Current"/> is always <c>null</c> and the
/// dispatcher shows every registered provider. A bridge package such as <c>Tharga.Team.Mcp</c> replaces
/// this service to derive the context from the authenticated request, which is what turns scope filtering
/// on. Register your own before <c>AddThargaMcp</c> to substitute a different identity model.
/// </remarks>
public interface IMcpContextAccessor
{
    /// <summary>The current call's context, or <c>null</c> when outside an MCP request.</summary>
    IMcpContext Current { get; set; }
}
