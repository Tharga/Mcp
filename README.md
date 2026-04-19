# Tharga.Mcp

Foundation package for [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) infrastructure in the Tharga ecosystem. Wraps the official [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) C# SDK with a Tharga-flavored registration pattern so downstream provider packages (`Tharga.MongoDB.Mcp`, `Tharga.Platform.Mcp`, etc.) compose cleanly inside a single `AddThargaMcp(...)` callback.

`Tharga.Mcp` itself has **no dependency on any other Tharga package** — the auth/scope/audit integration lives in `Tharga.Platform.Mcp` (Phase 1).

## Install

```bash
dotnet add package Tharga.Mcp
```

## Minimal usage

```csharp
using Tharga.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThargaMcp(mcp =>
{
    mcp.Services.AddMcpServer().WithTools<HelloTools>();
});

var app = builder.Build();
app.MapMcp();
app.Run();
```

`MapMcp()` exposes the MCP endpoint at `ThargaMcpOptions.EndpointBasePath` (default `/mcp`).

## Defining tools

Any class tagged with `[McpServerToolType]` exposing `[McpServerTool]` methods is recognised by the SDK:

```csharp
[McpServerToolType]
public sealed class HelloTools
{
    [McpServerTool, Description("Returns a greeting for the given name.")]
    public string Greet([Description("The name to greet.")] string name)
        => $"Hello, {name}!";
}
```

## Provider contracts

`Tharga.Mcp` also defines `IMcpResourceProvider` and `IMcpToolProvider` with per-scope registration (`User`, `Team`, `System`). This is the path for packages that need dynamic tools/resources — where the set of tools is known only at runtime (e.g. one MCP resource per MongoDB collection).

```csharp
public sealed class TimeToolProvider : IMcpToolProvider
{
    public McpScope Scope => McpScope.System;

    public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<McpToolDescriptor>>(
            [new McpToolDescriptor { Name = "time_now", Description = "Current UTC time." }]);

    public Task<McpToolResult> CallToolAsync(string name, JsonElement args, IMcpContext context, CancellationToken ct)
        => Task.FromResult(new McpToolResult
        {
            Content = [new McpContent { Text = DateTimeOffset.UtcNow.ToString("O") }],
        });
}

builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddToolProvider<TimeToolProvider>();
});
```

Provider packages expose an extension method on `IThargaMcpBuilder` so consumers can compose them inside the same callback:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddMongoDB();   // from Tharga.MongoDB.Mcp
    mcp.AddPlatform();  // from Tharga.Platform.Mcp
    mcp.AddToolProvider<MyCustomProvider>();
});
```

The two paths (attribute-based `[McpServerTool]` and contract-based `IMcpToolProvider`) work **side by side** — use attributes for statically-declared tools, providers for dynamic or programmatically-generated tools. Scope filtering (`/mcp/me`, `/mcp/team`, `/mcp/system`) activates in Phase 1 once `Tharga.Platform.Mcp` populates `IMcpContextAccessor.Current` from the authenticated request.

## Endpoint scopes

The master plan defines three scopes — `User` (`/mcp/me`), `Team` (`/mcp/team`), `System` (`/mcp/system`). **Phase 0 ships a single endpoint** (`/mcp`) that exposes the union of registered tools and resources; scope is carried on `IMcpContext.Scope` and enforced by providers. The three-endpoint split is deferred — see the master plan decision 2026-04-18.

## Sample

Runnable end-to-end sample lives under `Sample/Tharga.Mcp.Sample/`. Start it and connect with [`@modelcontextprotocol/inspector`](https://github.com/modelcontextprotocol/inspector):

```bash
cd Sample/Tharga.Mcp.Sample
dotnet run
# in another terminal:
npx @modelcontextprotocol/inspector http://localhost:5138/mcp
```

## License

MIT. See [LICENSE](./LICENSE).
