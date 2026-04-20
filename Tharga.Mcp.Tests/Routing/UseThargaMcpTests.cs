using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Tharga.Mcp.Tests.Routing;

public class UseThargaMcpTests
{
    [Fact]
    public async Task UseThargaMcp_maps_endpoint_at_default_base_path()
    {
        using var host = await BuildHostAsync(configureMcp: null, useObsoleteAlias: false);
        using var client = host.GetTestClient();

        var response = await client.PostAsync("/mcp", new StringContent(string.Empty));

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UseThargaMcp_maps_endpoint_at_configured_base_path()
    {
        using var host = await BuildHostAsync(configureMcp: mcp => mcp.Options.EndpointBasePath = "/api/mcp", useObsoleteAlias: false);
        using var client = host.GetTestClient();

        var defaultPath = await client.PostAsync("/mcp", new StringContent(string.Empty));
        var custom = await client.PostAsync("/api/mcp", new StringContent(string.Empty));

        defaultPath.StatusCode.Should().Be(HttpStatusCode.NotFound);
        custom.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Obsolete_MapMcp_alias_still_maps_the_endpoint()
    {
        using var host = await BuildHostAsync(configureMcp: null, useObsoleteAlias: true);
        using var client = host.GetTestClient();

        var response = await client.PostAsync("/mcp", new StringContent(string.Empty));

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequireAuth_true_adds_AuthorizeAttribute_metadata_to_the_endpoint()
    {
        using var host = await BuildHostAsync(configureMcp: mcp => mcp.Options.RequireAuth = true, useObsoleteAlias: false);

        var mcpEndpoints = GetMcpEndpoints(host);

        mcpEndpoints.Should().NotBeEmpty();
        mcpEndpoints.Should().OnlyContain(e => e.Metadata.GetMetadata<IAuthorizeData>() != null);
    }

    [Fact]
    public async Task RequireAuth_false_does_not_add_AuthorizeAttribute_metadata_to_the_endpoint()
    {
        using var host = await BuildHostAsync(configureMcp: mcp => mcp.Options.RequireAuth = false, useObsoleteAlias: false);

        var mcpEndpoints = GetMcpEndpoints(host);

        mcpEndpoints.Should().NotBeEmpty();
        mcpEndpoints.Should().OnlyContain(e => e.Metadata.GetMetadata<IAuthorizeData>() == null);
    }

    private static IReadOnlyList<Endpoint> GetMcpEndpoints(IHost host)
    {
        var sources = host.Services.GetServices<EndpointDataSource>();
        return sources
            .SelectMany(s => s.Endpoints)
            .Where(e => e is RouteEndpoint re && re.RoutePattern.RawText?.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
    }

    private static async Task<IHost> BuildHostAsync(Action<IThargaMcpBuilder> configureMcp, bool useObsoleteAlias)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddThargaMcp(mcp =>
                    {
                        mcp.Options.RequireAuth = false;
                        configureMcp?.Invoke(mcp);
                    });
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        if (useObsoleteAlias)
                        {
#pragma warning disable CS0618
                            endpoints.MapMcp();
#pragma warning restore CS0618
                        }
                        else
                        {
                            endpoints.UseThargaMcp();
                        }
                    });
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }
}
