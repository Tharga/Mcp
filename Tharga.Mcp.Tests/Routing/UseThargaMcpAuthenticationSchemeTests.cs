using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Tharga.Mcp.Tests.Routing;

/// <summary>
/// Which credential <see cref="ThargaMcpOptions.RequireAuth"/> actually accepts.
/// </summary>
/// <remarks>
/// MCP callers are agents presenting an API key — there is no user. Before
/// <see cref="ThargaMcpOptions.AuthenticationSchemes"/> existed, <c>RequireAuth</c> emitted a bare
/// <c>RequireAuthorization()</c>, whose policy names no scheme and therefore authenticates against the
/// application's <b>default</b> one. In any host with interactive sign-in that is OIDC, so the key was
/// never consulted and the agent was redirected to a login page (#18).
/// <para>
/// Asserted on the endpoint's authorization policy rather than over HTTP: the MCP endpoint negotiates
/// content before a credential is even relevant, so a status code says more about the request body than
/// about the policy under test.
/// </para>
/// </remarks>
public class UseThargaMcpAuthenticationSchemeTests
{
    private const string KeyScheme = "TestApiKeyScheme";

    /// <summary>The reported defect: no scheme is named, so the default one decides — and it never sees an API key.</summary>
    [Fact]
    public async Task Without_contributed_schemes_the_policy_names_none()
    {
        var policy = await GetPolicyAsync();

        policy.Should().NotBeNull();
        policy!.AuthenticationSchemes.Should().BeEmpty();
    }

    /// <summary>What a bridge package contributes on the host's behalf.</summary>
    [Fact]
    public async Task A_contributed_scheme_is_named_on_the_policy()
    {
        var policy = await GetPolicyAsync(o => o.AuthenticationSchemes.Add(KeyScheme));

        policy.Should().NotBeNull();
        policy!.AuthenticationSchemes.Should().ContainSingle().Which.Should().Be(KeyScheme);
    }

    /// <summary>A host adding its own alongside, so a signed-in user can explore what agents can reach.</summary>
    [Fact]
    public async Task Several_contributed_schemes_are_all_named()
    {
        var policy = await GetPolicyAsync(o =>
        {
            o.AuthenticationSchemes.Add(KeyScheme);
            o.AuthenticationSchemes.Add("Cookies");
        });

        policy!.AuthenticationSchemes.Should().BeEquivalentTo([KeyScheme, "Cookies"]);
    }

    /// <summary>Naming a scheme must not weaken the requirement — an anonymous caller is still refused.</summary>
    [Fact]
    public async Task The_policy_always_requires_an_authenticated_caller()
    {
        var policy = await GetPolicyAsync(o => o.AuthenticationSchemes.Add(KeyScheme));

        policy!.Requirements.Should().ContainSingle(r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task RequireAuth_false_applies_no_policy_at_all()
    {
        var policy = await GetPolicyAsync(o => o.RequireAuth = false);

        policy.Should().BeNull();
    }

    private static async Task<AuthorizationPolicy> GetPolicyAsync(Action<ThargaMcpOptions> configureOptions = null)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthorization();
                    services.AddThargaMcp(mcp => configureOptions?.Invoke(mcp.Options));
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.UseThargaMcp());
                });
            })
            .Build();

        await host.StartAsync();

        var endpoints = host.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        return endpoints
            .Select(e => e.Metadata.GetMetadata<AuthorizationPolicy>())
            .FirstOrDefault(p => p != null);
    }
}
