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

`Tharga.Mcp` also defines `IMcpResourceProvider` and `IMcpToolProvider` with per-scope registration (`User`, `Team`, `System`). Provider packages register via extension methods on `IThargaMcpBuilder`:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddMongoDB();   // from Tharga.MongoDB.Mcp
    mcp.AddPlatform();  // from Tharga.Platform.Mcp
});
```

The runtime bridge from these contracts to SDK-side tools/resources lands with the first real provider (MongoDB, Phase 2). Until then, register tools via the SDK's attribute pattern as shown above.

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
