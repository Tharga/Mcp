using System.Text.Json;
using Tharga.Mcp;

namespace Tharga.Mcp.Sample;

/// <summary>
/// Demonstrates the IMcpToolProvider path — tools exposed via Tharga's dynamic contract
/// rather than the SDK's [McpServerTool] attribute pattern. Both paths work side-by-side.
/// </summary>
public sealed class TimeToolProvider : IMcpToolProvider
{
    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
    {
        IReadOnlyList<McpToolDescriptor> tools =
        [
            new McpToolDescriptor { Name = "time_now", Description = "Returns the current UTC time in ISO-8601 format." },
        ];
        return Task.FromResult(tools);
    }

    public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
    {
        return toolName switch
        {
            "time_now" => Task.FromResult(new McpToolResult
            {
                Content = [new McpContent { Text = DateTimeOffset.UtcNow.ToString("O") }],
            }),
            _ => Task.FromResult(new McpToolResult
            {
                IsError = true,
                Content = [new McpContent { Text = $"Unknown tool: {toolName}" }],
            }),
        };
    }
}
