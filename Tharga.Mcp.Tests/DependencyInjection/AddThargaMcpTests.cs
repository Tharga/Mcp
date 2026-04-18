using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Mcp.Internal;
using Xunit;

namespace Tharga.Mcp.Tests.DependencyInjection;

public class AddThargaMcpTests
{
    [Fact]
    public void Invokes_configure_callback_with_builder()
    {
        var services = new ServiceCollection();
        IThargaMcpBuilder? captured = null;

        services.AddThargaMcp(mcp => captured = mcp);

        captured.Should().NotBeNull();
        captured!.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Context_accessor_is_registered_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddThargaMcp(_ => { });

        using var sp = services.BuildServiceProvider();
        var a1 = sp.GetRequiredService<IMcpContextAccessor>();
        var a2 = sp.GetRequiredService<IMcpContextAccessor>();

        a1.Should().BeSameAs(a2);
    }

    [Fact]
    public void Context_accessor_flows_through_async_local()
    {
        var services = new ServiceCollection();
        services.AddThargaMcp(_ => { });
        using var sp = services.BuildServiceProvider();
        var accessor = sp.GetRequiredService<IMcpContextAccessor>();

        accessor.Current = new TestContext(McpScope.User);

        accessor.Current.Should().NotBeNull();
        accessor.Current!.Scope.Should().Be(McpScope.User);
    }

    [Fact]
    public void Calling_twice_merges_registrations()
    {
        var services = new ServiceCollection();
        services.AddThargaMcp(mcp => mcp.AddToolProvider<NoopTools>());
        services.AddThargaMcp(mcp => mcp.AddResourceProvider<NoopResources>());

        using var sp = services.BuildServiceProvider();
        sp.GetServices<IMcpToolProvider>().Should().ContainSingle().Which.Should().BeOfType<NoopTools>();
        sp.GetServices<IMcpResourceProvider>().Should().ContainSingle().Which.Should().BeOfType<NoopResources>();
    }

    [Fact]
    public void Calling_twice_shares_the_same_registry_and_options()
    {
        var services = new ServiceCollection();
        McpProviderRegistry? first = null;
        ThargaMcpOptions? firstOpts = null;

        services.AddThargaMcp(mcp =>
        {
            first = services.BuildServiceProvider().GetRequiredService<McpProviderRegistry>();
            firstOpts = mcp.Options;
        });

        McpProviderRegistry? second = null;
        ThargaMcpOptions? secondOpts = null;
        services.AddThargaMcp(mcp =>
        {
            second = services.BuildServiceProvider().GetRequiredService<McpProviderRegistry>();
            secondOpts = mcp.Options;
        });

        second.Should().BeSameAs(first);
        secondOpts.Should().BeSameAs(firstOpts);
    }

    private sealed class NoopTools : IMcpToolProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([]);
        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [] });
    }

    private sealed class NoopResources : IMcpResourceProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpResourceDescriptor>>([]);
        public Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpResourceContent { Uri = uri });
    }

    private sealed record TestContext(McpScope Scope) : IMcpContext
    {
        public string? UserId => null;
        public string? TeamId => null;
        public bool IsDeveloper => false;
    }
}
