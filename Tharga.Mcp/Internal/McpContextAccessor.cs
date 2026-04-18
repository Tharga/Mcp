namespace Tharga.Mcp.Internal;

internal sealed class McpContextAccessor : IMcpContextAccessor
{
    private static readonly AsyncLocal<ContextHolder> _current = new();

    public IMcpContext Current
    {
        get => _current.Value?.Context;
        set
        {
            var holder = _current.Value;
            if (holder != null) holder.Context = null;
            if (value != null) _current.Value = new ContextHolder { Context = value };
        }
    }

    private sealed class ContextHolder
    {
        public IMcpContext Context;
    }
}
