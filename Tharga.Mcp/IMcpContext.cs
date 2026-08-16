namespace Tharga.Mcp;

/// <summary>
/// Per-call execution context surfaced to <see cref="IMcpResourceProvider"/> and <see cref="IMcpToolProvider"/> implementations.
/// </summary>
/// <remarks>
/// Carries the caller's privilege level and nothing else. With no bridge package registered there is no
/// context at all, and the dispatcher falls back to showing every provider; a bridge such as
/// <c>Tharga.Team.Mcp</c> supplies one per request from the authenticated principal and turns the filter on.
/// <para>
/// A provider needing to know <i>who</i> is calling — rather than at what level — must obtain that itself,
/// for example from <c>IHttpContextAccessor</c>. This contract deliberately carries no identity.
/// </para>
/// </remarks>
public interface IMcpContext
{
    /// <summary>
    /// The endpoint scope that served this call. This is the authorization signal on the context —
    /// the dispatcher shows a provider only when its <see cref="IMcpProvider.Scope"/> is at or below this value.
    /// </summary>
    McpScope Scope { get; }
}
