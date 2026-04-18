using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Tharga.Mcp.Tests.Routing;

public class MapMcpTests
{
    [Fact]
    public async Task MapMcp_maps_endpoint_at_default_base_path()
    {
        using var host = await BuildHostAsync(configure: null);
        using var client = host.GetTestClient();

        var response = await client.PostAsync("/mcp", new StringContent(string.Empty));

        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MapMcp_maps_endpoint_at_configured_base_path()
    {
        using var host = await BuildHostAsync(mcp => mcp.Options.EndpointBasePath = "/api/mcp");
        using var client = host.GetTestClient();

        var defaultPath = await client.PostAsync("/mcp", new StringContent(string.Empty));
        var custom = await client.PostAsync("/api/mcp", new StringContent(string.Empty));

        defaultPath.StatusCode.Should().Be(HttpStatusCode.NotFound);
        custom.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    private static async Task<IHost> BuildHostAsync(Action<IThargaMcpBuilder> configure)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddThargaMcp(mcp => configure?.Invoke(mcp));
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapMcp());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }
}
