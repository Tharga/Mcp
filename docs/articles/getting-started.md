# Getting started

## Install

```
dotnet add package Tharga.Mcp
```

Tharga.Mcp targets **.NET 10** and depends on the official [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) C# SDK.

## Minimal host

```csharp
using Tharga.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThargaMcp(mcp =>
{
    // Sample runs without authentication; flip to true in production.
    mcp.Options.RequireAuth = false;

    mcp.Services.AddMcpServer().WithTools<HelloTools>();
});

var app = builder.Build();
app.UseThargaMcp();
app.Run();
```

Define a tool with the SDK's attribute pattern:

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class HelloTools
{
    [McpServerTool, Description("Returns a greeting for the given name.")]
    public string Greet([Description("The name to greet.")] string name) => $"Hello, {name}!";
}
```

Run with `dotnet run` and connect from any MCP client (e.g. [MCP Inspector](https://github.com/modelcontextprotocol/inspector)):

```
npx @modelcontextprotocol/inspector
# Transport: Streamable HTTP
# URL: http://localhost:5000/mcp
```

## What `UseThargaMcp` does

`UseThargaMcp(IEndpointRouteBuilder)`:

1. Resolves `ThargaMcpOptions` from DI (filled in by your `AddThargaMcp(...)` callback).
2. Calls the SDK's `MapMcp(options.EndpointBasePath)` — default base path is `/mcp`.
3. If `options.RequireAuth == true` (the default), chains `.RequireAuthorization()` on the returned `IEndpointConventionBuilder`. See [Authorization](authorization.md) for the `UseAuthorization()` prerequisite.

The returned `IEndpointConventionBuilder` can be further chained — `app.UseThargaMcp().RequireAuthorization("SystemApiKeyPolicy")` stacks the policy.

## Adding a provider package

Provider packages (`Tharga.MongoDB.Mcp`, `Tharga.Platform.Mcp`, etc.) expose extension methods on `IThargaMcpBuilder`:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddPlatform();   // from Tharga.Platform.Mcp — wires Platform auth/scopes/audit
    mcp.AddMongoDB();    // from Tharga.MongoDB.Mcp — MongoDB monitoring/admin
});
```

Both attribute-based `[McpServerTool]` tools and contract-based `IMcpToolProvider` providers coexist on the same endpoint.

## Next steps

- [Defining providers](providers.md) — when to choose `IMcpToolProvider` over attributes.
- [Scopes](scopes.md) — how `User` / `Team` / `System` affect what each caller sees.
- [Authorization](authorization.md) — wiring auth middleware so `RequireAuth` actually enforces.
