# Authorization

`ThargaMcpOptions.RequireAuth` controls whether `UseThargaMcp()` chains `.RequireAuthorization()` on the mapped endpoint.

```csharp
public sealed class ThargaMcpOptions
{
    public string EndpointBasePath { get; set; } = "/mcp";
    public bool RequireAuth { get; set; } = true;  // default
}
```

When `RequireAuth = true`:

```csharp
public static IEndpointConventionBuilder UseThargaMcp(this IEndpointRouteBuilder endpoints)
{
    var options = endpoints.ServiceProvider.GetRequiredService<ThargaMcpOptions>();
    var conventionBuilder = endpoints.MapMcp(options.EndpointBasePath);

    if (options.RequireAuth)
    {
        conventionBuilder.RequireAuthorization();
    }

    return conventionBuilder;
}
```

The `.RequireAuthorization()` call without arguments applies the **default** authorization policy — which is just "authenticated user required". Additional `.RequireAuthorization("PolicyName")` calls stack rather than replace, so consumers can layer on top:

```csharp
app.UseThargaMcp().RequireAuthorization("SystemApiKeyPolicy");
```

## ⚠️ The `UseAuthorization()` prerequisite

Endpoints with auth metadata **throw at request time** if `UseAuthorization()` isn't in the ASP.NET Core pipeline:

> `InvalidOperationException: Endpoint contains authorization metadata, but a middleware was not found that supports authorization. Configure your application startup by adding app.UseAuthorization() inside the call to Configure(..) in the application startup code.`

ASP.NET Core does this intentionally — better a loud runtime exception than silently anonymous endpoints. Two ways to avoid it:

1. **Wire auth middleware** (the production path):

   ```csharp
   builder.Services.AddAuthentication("YourScheme").AddYourScheme(/* … */);
   builder.Services.AddAuthorization();

   builder.Services.AddThargaMcp(/* default RequireAuth = true */);

   var app = builder.Build();
   app.UseAuthentication();
   app.UseAuthorization();
   app.UseThargaMcp();
   ```

2. **Opt out for demos / tests**:

   ```csharp
   builder.Services.AddThargaMcp(mcp =>
   {
       mcp.Options.RequireAuth = false;  // anonymous /mcp endpoint
   });
   ```

The Tharga.Mcp sample uses path #2 with an explanatory comment, since it's a no-auth demo. Production consumers — especially Tharga.Platform.Mcp users — keep the `true` default.

## With Tharga.Platform.Mcp

`Tharga.Platform.Mcp`'s `AddPlatform()` extension wires `AddAuthentication`, `AddAuthorization`, the API-key + OIDC schemes, and an `IMcpContextAccessor` implementation that populates `Current` from `HttpContext.User`. All you have to do as a consumer is:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddPlatform();
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseThargaMcp();  // RequireAuth is true by default — Platform.Mcp wires the middleware
```

That gives you:

- Auth enforced on `/mcp` (no policy → default "authenticated user required").
- `IMcpContext` populated per-request from claims (see [Scopes](scopes.md) for how claims map to `McpScope`).
- Audit hooks via Tharga.Platform's `CompositeAuditLogger`.

## Test pattern

When writing integration tests, set `RequireAuth = false` in your test host helper unless the test is specifically asserting auth:

```csharp
services.AddThargaMcp(mcp =>
{
    mcp.Options.RequireAuth = false;
    // test-specific configuration
});
```

For tests that *do* assert the auth metadata is wired (without spinning up a full auth pipeline), inspect `EndpointDataSource.Endpoints` directly:

```csharp
var endpoints = host.Services.GetServices<EndpointDataSource>()
    .SelectMany(s => s.Endpoints)
    .Where(e => e is RouteEndpoint re && re.RoutePattern.RawText?.StartsWith("/mcp") == true);

endpoints.Should().OnlyContain(e => e.Metadata.GetMetadata<IAuthorizeData>() != null);
```

`Tharga.Mcp.Tests/Routing/UseThargaMcpTests` has two tests in this shape — one for the `true` case, one for the `false` case.
