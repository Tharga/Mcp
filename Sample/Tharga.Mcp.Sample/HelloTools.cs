using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Tharga.Mcp.Sample;

[McpServerToolType]
public sealed class HelloTools
{
    [McpServerTool, Description("Returns a greeting for the given name.")]
    public string Greet([Description("The name to greet.")] string name)
        => $"Hello, {name}!";

    [McpServerTool, Description("Echoes the input back.")]
    public string Echo([Description("The message to echo.")] string message)
        => $"Echo: {message}";
}
