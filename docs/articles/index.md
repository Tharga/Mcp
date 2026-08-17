# Articles

Guides for using Tharga.Mcp.

- **[Getting started](getting-started.md)** — install, minimal `AddThargaMcp` / `UseThargaMcp` setup, hosting in ASP.NET Core.
- **[Defining providers](providers.md)** — `IMcpToolProvider` / `IMcpResourceProvider`, the `McpScope` value, and how dispatch works alongside the SDK's `[McpServerTool]` attribute path.
- **[Scopes](scopes.md)** — the User/Team/System hierarchy filter and how `Tharga.Team.Mcp` populates the per-request context.
- **[Authorization](authorization.md)** — the `RequireAuth` flag, naming schemes with `AuthenticationSchemes` so an API-key caller is accepted, the `UseAuthorization()` prerequisite, and how policies stack on top.
- **[Session mode](session-mode.md)** — the `SessionMode` option, why the transport is stateless by default, and how to keep serving clients that still expect an `Mcp-Session-Id`.
