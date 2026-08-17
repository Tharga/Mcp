# Authorization

Two options govern the endpoint's authorization: `RequireAuth` decides *whether* a caller must be authenticated, and `AuthenticationSchemes` decides *which credentials* count.

```csharp
public sealed class ThargaMcpOptions
{
    public string EndpointBasePath { get; set; } = "/mcp";
    public bool RequireAuth { get; set; } = true;  // default
    public IList<string> AuthenticationSchemes { get; } = [];
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
        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes([.. options.AuthenticationSchemes])
            .RequireAuthenticatedUser()
            .Build();

        conventionBuilder.RequireAuthorization(policy);
    }

    return conventionBuilder;
}
```

Additional `.RequireAuthorization("PolicyName")` calls stack rather than replace, so consumers can layer on top:

```csharp
app.UseThargaMcp().RequireAuthorization("SystemApiKeyPolicy");
```

Because they stack with AND, a second policy **narrows** access — it cannot widen it. Adding one is not a way to make another credential acceptable; for that, name the scheme (below).

## Which credential is accepted

`AuthenticationSchemes` is empty by default, and an empty list means the endpoint authenticates against the application's **default scheme**.

That default is the thing to watch. In a host with interactive sign-in the default scheme is OIDC or cookies, so an MCP caller presenting an API key is not merely rejected — it is *challenged*, and answers with a `302` to a login page:

```
POST /mcp  with  X-API-KEY: <valid key>
→ HTTP 302 to login.microsoftonline.com
```

MCP callers are agents. There is normally no user to sign in, so an API key is the expected credential — which makes this the one configuration that has to work. Name the scheme and it does:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.Options.AuthenticationSchemes.Add(ApiKeyConstants.SchemeName);
});
```

Add more than one where more than one credential should be accepted — an agent with a key and a signed-in user exploring the same endpoint:

```csharp
mcp.Options.AuthenticationSchemes.Add(ApiKeyConstants.SchemeName);
mcp.Options.AuthenticationSchemes.Add(CookieAuthenticationDefaults.AuthenticationScheme);
```

Naming schemes never weakens the requirement: an anonymous caller is still refused. It only decides which handlers get to examine the request.

Bridge packages contribute the schemes their callers use, so a host that registers one need not know about schemes at all — `Tharga.Team.Mcp`'s `AddTeam()` adds the API-key scheme itself. A host is free to add its own alongside.

> **Upgrading:** `AuthenticationSchemes` is additive. An empty list produces exactly the previous behavior, so nothing changes for an existing host until it (or a bridge package) adds a scheme.

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

The Tharga.Mcp sample uses path #2 with an explanatory comment, since it's a no-auth demo. Production consumers — especially `Tharga.Team.Mcp` users — keep the `true` default.

## With Tharga.Team.Mcp

`Tharga.Team.Mcp`'s `AddTeam()` extension wires `AddAuthentication`, `AddAuthorization`, the API-key + OIDC schemes, and an `IMcpContextAccessor` implementation that derives `Current` from `HttpContext.User`. All you have to do as a consumer is:

```csharp
builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddTeam();
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseThargaMcp();  // RequireAuth is true by default — AddTeam contributes the schemes
```

That gives you:

- Auth enforced on `/mcp`, against whichever schemes the bridge contributed (the default scheme when it contributed none).
- `IMcpContext` populated per-request from claims (see [Scopes](scopes.md) for how claims map to `McpScope`).
- Audit hooks via Tharga.Team's `CompositeAuditLogger`.

> `Tharga.Platform.Mcp` was the earlier name for this bridge and is deprecated, frozen at 3.5.4. `mcp.AddPlatform()` is superseded by `mcp.AddTeam()`.

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

To assert *which* schemes the policy names, read the `AuthorizationPolicy` off the endpoint rather than sending a request — the MCP endpoint negotiates content before a credential is relevant, so a status code describes the request body more than the policy:

```csharp
var policy = host.Services.GetRequiredService<EndpointDataSource>().Endpoints
    .Select(e => e.Metadata.GetMetadata<AuthorizationPolicy>())
    .FirstOrDefault(p => p != null);

policy.AuthenticationSchemes.Should().ContainSingle().Which.Should().Be(ApiKeyConstants.SchemeName);
```

`Tharga.Mcp.Tests/Routing/UseThargaMcpAuthenticationSchemeTests` covers this.

To assert the *behavior* instead — that a key-bearing agent is answered rather than redirected — register a stand-in for each scheme (one that challenges with a `302`, one that accepts a header) and post an `initialize` request through `TestServer`. `Tharga.Mcp.Tests/Routing/UseThargaMcpApiKeyTests` does that, and is the test that fails if the endpoint ever falls back to the default scheme again.
