using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Tharga.Mcp.Tests.Contracts;

public class ProviderContractTests
{
    [Fact]
    public void Resource_provider_declares_scope_and_surfaces_contents()
    {
        IMcpResourceProvider provider = new FakeResourceProvider(McpScope.System, [new McpResourceDescriptor { Uri = "hello://one", Name = "One" }]);

        provider.Scope.Should().Be(McpScope.System);
    }

    [Fact]
    public async Task Resource_provider_returns_declared_resources_and_content()
    {
        var descriptor = new McpResourceDescriptor { Uri = "hello://one", Name = "One", MimeType = "text/plain" };
        IMcpResourceProvider provider = new FakeResourceProvider(McpScope.User, [descriptor]);
        var context = FakeContext.System();

        var list = await provider.ListResourcesAsync(context, default);
        list.Should().ContainSingle().Which.Should().BeEquivalentTo(descriptor);

        var content = await provider.ReadResourceAsync(descriptor.Uri, context, default);
        content.Uri.Should().Be(descriptor.Uri);
        content.Text.Should().Be("payload for hello://one");
    }

    [Fact]
    public async Task Tool_provider_returns_declared_tools_and_echoes_arguments()
    {
        var tool = new McpToolDescriptor { Name = "echo", Description = "Echoes input." };
        IMcpToolProvider provider = new FakeToolProvider(McpScope.Team, [tool]);
        var context = FakeContext.System();

        var list = await provider.ListToolsAsync(context, default);
        list.Should().ContainSingle().Which.Name.Should().Be("echo");

        using var args = JsonDocument.Parse("""{"msg":"hi"}""");
        var result = await provider.CallToolAsync("echo", args.RootElement, context, default);

        result.IsError.Should().BeFalse();
        result.Content.Should().ContainSingle().Which.Text.Should().Contain("hi");
    }

    private sealed class FakeResourceProvider(McpScope scope, IReadOnlyList<McpResourceDescriptor> resources) : IMcpResourceProvider
    {
        public McpScope Scope { get; } = scope;

        public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(resources);

        public Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpResourceContent { Uri = uri, Text = $"payload for {uri}" });
    }

    private sealed class FakeToolProvider(McpScope scope, IReadOnlyList<McpToolDescriptor> tools) : IMcpToolProvider
    {
        public McpScope Scope { get; } = scope;

        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(tools);

        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [new McpContent { Text = arguments.GetRawText() }] });
    }

    private sealed record FakeContext(string UserId, string TeamId, bool IsDeveloper, McpScope Scope) : IMcpContext
    {
        public static FakeContext System() => new(UserId: "u1", TeamId: null, IsDeveloper: true, Scope: McpScope.System);
    }
}
