using System.ComponentModel;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Xunit;

namespace Tharga.Mcp.Tests.Bridge;

public class ProviderBridgeTests
{
    [Fact]
    public async Task Tool_provider_is_discoverable_and_callable_through_the_SDK()
    {
        using var host = await BuildHostAsync(mcp => mcp.AddToolProvider<EchoToolProvider>());
        using var client = await ConnectAsync(host);

        var list = await client.SendAsync("tools/list");
        var tools = list.GetProperty("result").GetProperty("tools").EnumerateArray().ToList();
        tools.Should().ContainSingle();
        tools[0].GetProperty("name").GetString().Should().Be("echo");

        var call = await client.SendAsync("tools/call", new { name = "echo", arguments = new { message = "ping" } });
        var content = call.GetProperty("result").GetProperty("content").EnumerateArray().First();
        content.GetProperty("text").GetString().Should().Be("pong: ping");
    }

    [Fact]
    public async Task Resource_provider_is_discoverable_and_readable_through_the_SDK()
    {
        using var host = await BuildHostAsync(mcp => mcp.AddResourceProvider<GreetingResourceProvider>());
        using var client = await ConnectAsync(host);

        var list = await client.SendAsync("resources/list");
        var resources = list.GetProperty("result").GetProperty("resources").EnumerateArray().ToList();
        resources.Should().ContainSingle();
        resources[0].GetProperty("uri").GetString().Should().Be("hello://greeting");

        var read = await client.SendAsync("resources/read", new { uri = "hello://greeting" });
        var contents = read.GetProperty("result").GetProperty("contents").EnumerateArray().First();
        contents.GetProperty("text").GetString().Should().Be("hello from a provider");
    }

    [Fact]
    public async Task Unknown_tool_name_returns_an_isError_result()
    {
        using var host = await BuildHostAsync(mcp => mcp.AddToolProvider<EchoToolProvider>());
        using var client = await ConnectAsync(host);

        var call = await client.SendAsync("tools/call", new { name = "does-not-exist", arguments = new { } });
        var result = call.GetProperty("result");
        result.GetProperty("isError").GetBoolean().Should().BeTrue();
        result.GetProperty("content").EnumerateArray().First().GetProperty("text").GetString()
            .Should().Contain("does-not-exist");
    }

    [Fact]
    public async Task Attribute_based_tools_and_provider_based_tools_coexist()
    {
        using var host = await BuildHostAsync(mcp =>
        {
            mcp.Services.AddMcpServer().WithTools<AttributeTools>();
            mcp.AddToolProvider<EchoToolProvider>();
        });
        using var client = await ConnectAsync(host);

        var list = await client.SendAsync("tools/list");
        var names = list.GetProperty("result").GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();
        names.Should().Contain(["echo", "attribute_greet"]);

        var echo = await client.SendAsync("tools/call", new { name = "echo", arguments = new { message = "hi" } });
        echo.GetProperty("result").GetProperty("content").EnumerateArray().First().GetProperty("text").GetString()
            .Should().Be("pong: hi");

        var greet = await client.SendAsync("tools/call", new { name = "attribute_greet", arguments = new { who = "world" } });
        greet.GetProperty("result").GetProperty("content").EnumerateArray().First().GetProperty("text").GetString()
            .Should().Be("Hello, world!");
    }

    [Fact]
    public async Task Provider_receives_the_arguments_as_a_json_object()
    {
        using var host = await BuildHostAsync(mcp => mcp.AddToolProvider<EchoArgumentsProvider>());
        using var client = await ConnectAsync(host);

        var call = await client.SendAsync("tools/call", new { name = "echo_args", arguments = new { a = 1, b = "two", c = true } });
        var text = call.GetProperty("result").GetProperty("content").EnumerateArray().First().GetProperty("text").GetString();
        using var parsed = JsonDocument.Parse(text);
        parsed.RootElement.GetProperty("a").GetInt32().Should().Be(1);
        parsed.RootElement.GetProperty("b").GetString().Should().Be("two");
        parsed.RootElement.GetProperty("c").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task User_scope_caller_sees_only_user_scope_providers()
    {
        // Hierarchy filter: User (0) <= User (0) → user_tool visible.
        // System (2) > User (0) → system_tool hidden.
        var names = await ListToolNamesUnderScopeAsync(McpScope.User);

        names.Should().Contain("user_tool");
        names.Should().NotContain("system_tool");
    }

    [Fact]
    public async Task Team_scope_caller_sees_user_and_team_providers_but_not_system()
    {
        // Team (1) caller: User (0) <= 1 ✓, Team (1) <= 1 ✓, System (2) > 1 ✗.
        var names = await ListToolNamesUnderScopeAsync(McpScope.Team, registerTeam: true);

        names.Should().Contain(["user_tool", "team_tool"]);
        names.Should().NotContain("system_tool");
    }

    [Fact]
    public async Task System_scope_caller_sees_providers_from_all_scopes()
    {
        // System (2) caller: User (0) ✓, Team (1) ✓, System (2) ✓.
        var names = await ListToolNamesUnderScopeAsync(McpScope.System, registerTeam: true);

        names.Should().Contain(["user_tool", "team_tool", "system_tool"]);
    }

    private static async Task<List<string>> ListToolNamesUnderScopeAsync(McpScope callerScope, bool registerTeam = false)
    {
        using var host = await BuildHostAsync(
            configureMcp: mcp =>
            {
                mcp.AddToolProvider<UserScopeTool>();
                if (registerTeam) mcp.AddToolProvider<TeamScopeTool>();
                mcp.AddToolProvider<SystemScopeTool>();
            },
            configureApp: app =>
            {
                app.Use(async (httpContext, next) =>
                {
                    var accessor = httpContext.RequestServices.GetRequiredService<IMcpContextAccessor>();
                    accessor.Current = new TestContext(callerScope);
                    try { await next(); }
                    finally { accessor.Current = null; }
                });
            });
        using var client = await ConnectAsync(host);

        var list = await client.SendAsync("tools/list");
        return list.GetProperty("result").GetProperty("tools")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();
    }

    private static Task<IHost> BuildHostAsync(Action<IThargaMcpBuilder> configure)
        => BuildHostAsync(configure, configureApp: null);

    private static async Task<IHost> BuildHostAsync(Action<IThargaMcpBuilder> configureMcp, Action<IApplicationBuilder> configureApp)
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
                    configureApp?.Invoke(app);
                    app.UseEndpoints(endpoints => endpoints.UseThargaMcp());
                });
            })
            .Build();
        await host.StartAsync();
        return host;
    }

    private static async Task<McpJsonRpcTestClient> ConnectAsync(IHost host)
    {
        var client = new McpJsonRpcTestClient(host.GetTestClient());
        await client.InitializeAsync();
        return client;
    }

    private sealed class EchoToolProvider : IMcpToolProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([new McpToolDescriptor { Name = "echo", Description = "Replies pong: <message>." }]);

        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
        {
            var message = arguments.TryGetProperty("message", out var m) ? m.GetString() : "";
            return Task.FromResult(new McpToolResult { Content = [new McpContent { Text = $"pong: {message}" }] });
        }
    }

    private sealed class EchoArgumentsProvider : IMcpToolProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([new McpToolDescriptor { Name = "echo_args" }]);

        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [new McpContent { Text = arguments.GetRawText() }] });
    }

    private sealed class GreetingResourceProvider : IMcpResourceProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpResourceDescriptor>> ListResourcesAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpResourceDescriptor>>([new McpResourceDescriptor { Uri = "hello://greeting", Name = "Greeting" }]);

        public Task<McpResourceContent> ReadResourceAsync(string uri, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpResourceContent { Uri = uri, Text = "hello from a provider" });
    }

    private sealed class UserScopeTool : IMcpToolProvider
    {
        public McpScope Scope => McpScope.User;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([new McpToolDescriptor { Name = "user_tool" }]);
        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [new McpContent { Text = "user" }] });
    }

    private sealed class TeamScopeTool : IMcpToolProvider
    {
        public McpScope Scope => McpScope.Team;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([new McpToolDescriptor { Name = "team_tool" }]);
        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [new McpContent { Text = "team" }] });
    }

    private sealed class SystemScopeTool : IMcpToolProvider
    {
        public McpScope Scope => McpScope.System;
        public Task<IReadOnlyList<McpToolDescriptor>> ListToolsAsync(IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<McpToolDescriptor>>([new McpToolDescriptor { Name = "system_tool" }]);
        public Task<McpToolResult> CallToolAsync(string toolName, JsonElement arguments, IMcpContext context, CancellationToken cancellationToken)
            => Task.FromResult(new McpToolResult { Content = [new McpContent { Text = "system" }] });
    }

    [McpServerToolType]
    public sealed class AttributeTools
    {
        [McpServerTool(Name = "attribute_greet"), Description("Greets someone.")]
        public string Greet([Description("Who to greet.")] string who) => $"Hello, {who}!";
    }

    private sealed record TestContext(McpScope Scope) : IMcpContext
    {
        public string UserId => null;
        public string TeamId => null;
        public bool IsDeveloper => false;
    }
}
