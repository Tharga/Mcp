using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using Xunit;

namespace Tharga.Mcp.Tests.Transport;

public class SessionModeTests
{
    [Fact]
    public void SessionMode_defaults_to_stateless()
    {
        var options = new ThargaMcpOptions();

        options.SessionMode.Should().Be(McpSessionMode.Stateless);
    }

    [Fact]
    public void A_host_that_configures_nothing_gets_a_stateless_transport()
    {
        var transport = ResolveTransportOptions(configureMcp: null);

        transport.SessionMode.Should().Be(HttpServerSessionMode.Stateless);
        transport.Stateless.Should().BeTrue();
    }

    [Theory]
    [InlineData(McpSessionMode.Stateless, HttpServerSessionMode.Stateless)]
    [InlineData(McpSessionMode.Stateful, HttpServerSessionMode.Stateful)]
    [InlineData(McpSessionMode.StatefulForInitializeClients, HttpServerSessionMode.StatefulForInitializeClients)]
    public void The_configured_session_mode_reaches_the_transport(McpSessionMode configured, HttpServerSessionMode expected)
    {
        var transport = ResolveTransportOptions(mcp => mcp.Options.SessionMode = configured);

        transport.SessionMode.Should().Be(expected);
    }

    [Fact]
    public void StatefulForInitializeClients_survives_the_round_trip()
    {
        var transport = ResolveTransportOptions(mcp => mcp.Options.SessionMode = McpSessionMode.StatefulForInitializeClients);

        transport.SessionMode.Should().Be(HttpServerSessionMode.StatefulForInitializeClients);
        transport.Stateless.Should().BeFalse("the SDK's bool cannot represent the hybrid mode, which is why the option is an enum");
    }

    [Fact]
    public void Every_McpSessionMode_maps_to_the_SDK_member_of_the_same_name()
    {
        var unmapped = Enum.GetValues<McpSessionMode>()
            .Where(mode => ResolveTransportOptions(mcp => mcp.Options.SessionMode = mode).SessionMode.ToString() != mode.ToString())
            .ToList();

        unmapped.Should().BeEmpty("a new McpSessionMode member must be mapped in McpTypeMappers");
    }

    [Fact]
    public void Both_enums_declare_the_same_members()
    {
        var ours = Enum.GetNames<McpSessionMode>().OrderBy(x => x);
        var sdk = Enum.GetNames<HttpServerSessionMode>().OrderBy(x => x);

        ours.Should().Equal(sdk, "an SDK member with no counterpart is a mode a host cannot select");
    }

    private static HttpServerTransportOptions ResolveTransportOptions(Action<IThargaMcpBuilder> configureMcp)
    {
        var services = new ServiceCollection();
        services.AddThargaMcp(mcp => configureMcp?.Invoke(mcp));

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;
    }
}
