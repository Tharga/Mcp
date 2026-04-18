using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tharga.Mcp.Internal;
using Xunit;

namespace Tharga.Mcp.Tests.Builder;

public class ThargaMcpBuilderTests
{
    [Fact]
    public void AddResourceProvider_registers_once_even_if_called_twice()
    {
        var (builder, registry) = CreateBuilder();

        builder.AddResourceProvider<SampleResourceProvider>();
        builder.AddResourceProvider<SampleResourceProvider>();

        registry.ResourceProviders.Should().ContainSingle();
        builder.Services.Count(d => d.ServiceType == typeof(IMcpResourceProvider)).Should().Be(1);
    }

    [Fact]
    public void AddToolProvider_registers_once_even_if_called_twice()
    {
        var (builder, registry) = CreateBuilder();

        builder.AddToolProvider<SampleToolProvider>();
        builder.AddToolProvider<SampleToolProvider>();

        registry.ToolProviders.Should().ContainSingle();
        builder.Services.Count(d => d.ServiceType == typeof(IMcpToolProvider)).Should().Be(1);
    }

    [Fact]
    public void Providers_registered_on_builder_are_resolvable_from_the_container()
    {
        var (builder, _) = CreateBuilder();
        builder.AddResourceProvider<SampleResourceProvider>();
        builder.AddToolProvider<SampleToolProvider>();

        using var provider = builder.Services.BuildServiceProvider();

        provider.GetServices<IMcpResourceProvider>().Should().ContainSingle().Which.Should().BeOfType<SampleResourceProvider>();
        provider.GetServices<IMcpToolProvider>().Should().ContainSingle().Which.Should().BeOfType<SampleToolProvider>();
    }

    [Fact]
    public void Options_mutated_on_builder_are_visible_after_registration()
    {
        var (builder, _) = CreateBuilder();

        builder.Options.EndpointBasePath = "/api/mcp";
        builder.Options.RequireAuth = false;

        builder.Options.EndpointBasePath.Should().Be("/api/mcp");
        builder.Options.RequireAuth.Should().BeFalse();
    }

    [Fact]
    public void Multiple_different_providers_at_the_same_scope_both_register()
    {
        var (builder, registry) = CreateBuilder();

        builder.AddToolProvider<SampleToolProvider>();
        builder.AddToolProvider<AnotherToolProvider>();

        registry.ToolProviders.Should().HaveCount(2);
        using var provider = builder.Services.BuildServiceProvider();
        provider.GetServices<IMcpToolProvider>().Should().HaveCount(2);
    }

    private static (IThargaMcpBuilder Builder, McpProviderRegistry Registry) CreateBuilder()
    {
        var services = new ServiceCollection();
        var options = new ThargaMcpOptions();
        var registry = new McpProviderRegistry();
        return (new ThargaMcpBuilder(services, options, registry), registry);
    }

    private sealed class SampleResourceProvider : IMcpResourceProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpResourceDescriptor>>([]);
        public Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpResourceContent { Uri = uri });
    }

    private sealed class SampleToolProvider : IMcpToolProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([]);
        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [] });
    }

    private sealed class AnotherToolProvider : IMcpToolProvider
    {
        public McpScope Scope => McpScope.User;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([]);
        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [] });
    }
}
