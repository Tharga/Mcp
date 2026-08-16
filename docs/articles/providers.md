# Defining providers

Tharga.Mcp gives consumers two ways to expose tools and resources, and they coexist on the same endpoint:

| Path | When to choose |
|---|---|
| `[McpServerTool]` attribute on a method | Static set of tools known at compile time. The SDK's recommended path. |
| `IMcpToolProvider` / `IMcpResourceProvider` | Tools and resources discovered at runtime — e.g. one resource per MongoDB collection, one tool per Fortnox table. |

Attribute-based tools take priority during dispatch; provider methods run only for names the attribute collection doesn't claim.

## Implementing a tool provider

```csharp
using System.Text.Json;
using Tharga.Mcp;

public sealed class TimeToolProvider : IMcpToolProvider
{
    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<McpToolDescriptor>>(
            [new McpToolDescriptor
            {
                Name = "time_now",
                Description = "Returns the current UTC time in ISO-8601 format.",
            }]);

    public Task<McpToolResult> CallToolAsync(string name, JsonElement args, IMcpContext context, CancellationToken ct)
    {
        return name switch
        {
            "time_now" => Task.FromResult(new McpToolResult
            {
                Content = [new McpContent { Text = DateTimeOffset.UtcNow.ToString("O") }],
            }),
            _ => Task.FromResult(new McpToolResult
            {
                IsError = true,
                Content = [new McpContent { Text = $"Unknown tool: {name}" }],
            }),
        };
    }
}
```

Register it:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddToolProvider<TimeToolProvider>();
});
```

## Implementing a resource provider

```csharp
public sealed class GreetingResourceProvider : IMcpResourceProvider
{
    public McpScope Scope => McpScope.User;

    public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<McpResourceDescriptor>>(
            [new McpResourceDescriptor { Uri = "hello://greeting", Name = "Greeting" }]);

    public Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken ct)
        => Task.FromResult(new McpResourceContent
        {
            Uri = uri,
            Text = $"hello {context.UserId ?? "anonymous"}",
        });
}

// Registration
mcp.AddResourceProvider<GreetingResourceProvider>();
```

## How dispatch works

When a client calls `tools/list` or `tools/call`, the internal `McpProviderDispatcher`:

1. Reads `IMcpContextAccessor.Current` from the per-request service provider.
2. Filters registered providers by **scope hierarchy** (`p.Scope <= current.Scope`) — see [Scopes](scopes.md).
3. For `tools/list`: aggregates `ListToolsAsync` results from every surviving provider into `ListToolsResult`.
4. For `tools/call`: walks providers in registration order, asks each for its current tool list, and delegates `CallToolAsync` to the first one that claims the requested name. Unknown tool → `CallToolResult { IsError = true }`.

Resources work the same way (`ListResourcesAsync` / `ReadResourceAsync`); unknown URI throws.

The dispatcher fills `McpServerOptions.Handlers.{ListTools,CallTool,ListResources,ReadResource}Handler` slots that the SDK leaves null by default — using `??=`, so it never overwrites a handler a consumer has already set.

## The `IMcpContext` parameter

Every provider method receives an `IMcpContext`:

```csharp
public interface IMcpContext
{
    string UserId { get; }
    string TeamId { get; }
    McpScope Scope { get; }
}
```

`UserId` and `TeamId` are identity data for the provider to use — the foundation itself never reads them. `Scope` is the authorization signal, and the only member the dispatcher consults.

In a `Tharga.Mcp`-only host (no bridge package), `Current` is `null` and providers receive a fallback context with `Scope = System` and `UserId` / `TeamId` null. Note what that means: with no bridge, scope filtering is skipped entirely and **every** provider is visible. Wiring a bridge (`Tharga.Team.Mcp`) is what populates `Current` per-request from the authenticated principal and turns the filter on.

> **Removed in 2.0.0:** `bool IsDeveloper`. It named a role the host configures and could rename, nothing in this package read it, and its documentation wrongly claimed it gated the `System` endpoint — `Scope` does, via the hierarchy filter. Gate on `Scope` instead, or on your own bridge's scope check.

## Choosing between attributes and providers

Use **attributes** when:

- The tool set is fixed at compile time.
- The parameters are known types — the SDK auto-generates the JSON schema from your method signature.

Use **`IMcpToolProvider`** when:

- The tool set depends on runtime data (e.g. one tool per MongoDB collection).
- You need to short-circuit `ListToolsAsync` based on context (skip a tool if the caller can't see it).
- The provider package is meant to be composable — consumers can declare *what* providers they want via `mcp.AddX()` and not have to import your tool classes.

Both paths can coexist in the same `AddThargaMcp(...)` block.
