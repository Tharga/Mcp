---
_layout: landing
---

# Tharga.Mcp

Foundation package for [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) infrastructure in the Tharga ecosystem. Wraps the official [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) C# SDK with a Tharga-flavored registration pattern so downstream provider packages (`Tharga.MongoDB.Mcp`, `Tharga.Platform.Mcp`, etc.) compose cleanly inside a single `AddThargaMcp(...)` callback. Built for **.NET 8 / 9 / 10**.

## Package

| Package | What it does |
|---|---|
| [Tharga.Mcp](https://www.nuget.org/packages/Tharga.Mcp) | Contracts (`IMcpToolProvider`, `IMcpResourceProvider`, `IMcpContext`, `McpScope`), the `IThargaMcpBuilder` registration pattern, and `UseThargaMcp()` endpoint mapping. No Tharga dependencies — auth/scopes/audit integration lives in `Tharga.Platform.Mcp`. |

## Quick start

```
dotnet add package Tharga.Mcp
```

```csharp
using Tharga.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThargaMcp(mcp =>
{
    mcp.Services.AddMcpServer().WithTools<HelloTools>();
});

var app = builder.Build();
app.UseThargaMcp();
app.Run();
```

`UseThargaMcp()` exposes the MCP endpoint at `ThargaMcpOptions.EndpointBasePath` (default `/mcp`). It honors `ThargaMcpOptions.RequireAuth` (default `true`); see [Authorization](articles/authorization.md).

## What's in the box

- **Provider contracts** — `IMcpToolProvider` and `IMcpResourceProvider` for runtime-discovered tools and resources, alongside the SDK's attribute-based `[McpServerTool]` pattern. See [Defining providers](articles/providers.md).
- **Scope hierarchy** — `User` < `Team` < `System`. A System caller sees everything; Team sees User + Team; User sees only User. See [Scopes](articles/scopes.md).
- **Auto-authorization** — `RequireAuth = true` chains `.RequireAuthorization()` on the mapped endpoint. See [Authorization](articles/authorization.md).
- **Coexistence with attribute-based tools** — both registration paths work side-by-side in the same server.

## Repo

[github.com/Tharga/Mcp](https://github.com/Tharga/Mcp) — source, issues, releases.
