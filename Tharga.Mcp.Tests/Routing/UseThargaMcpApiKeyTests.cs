using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tharga.Mcp.Tests.Bridge;
using Xunit;

namespace Tharga.Mcp.Tests.Routing;

/// <summary>
/// The defect in Tharga/Mcp#18 as it was reported — over HTTP, in a host shaped like the one that hit it:
/// interactive sign-in as the default scheme, an API key alongside, and an agent presenting that key.
/// </summary>
/// <remarks>
/// <see cref="UseThargaMcpAuthenticationSchemeTests"/> asserts which schemes the policy names. These assert
/// what a caller actually gets back, which is what the report described: a <c>302</c> to a login page rather
/// than an answer.
/// </remarks>
public class UseThargaMcpApiKeyTests
{
    private const string InteractiveScheme = "TestInteractive";
    private const string KeyScheme = "TestApiKey";
    private const string KeyHeader = "X-API-KEY";
    private const string ValidKey = "valid-key";
    private const string LoginPage = "https://login.example.com/signin";

    /// <summary>The reported symptom: the key is never consulted, because the default scheme answers first.</summary>
    [Fact]
    public async Task Without_contributed_schemes_a_valid_key_is_redirected_to_the_login_page()
    {
        using var host = await BuildHostAsync();

        using var response = await PostInitializeAsync(host, ValidKey);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().Be(LoginPage);
    }

    /// <summary>The fix: name the scheme and the same request is answered.</summary>
    [Fact]
    public async Task A_contributed_key_scheme_lets_the_same_request_through()
    {
        using var host = await BuildHostAsync(o => o.AuthenticationSchemes.Add(KeyScheme));

        using var response = await PostInitializeAsync(host, ValidKey);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    /// <summary>Naming a scheme must not open the endpoint: no key is still no entry.</summary>
    [Fact]
    public async Task A_caller_without_a_key_is_still_refused()
    {
        using var host = await BuildHostAsync(o => o.AuthenticationSchemes.Add(KeyScheme));

        using var response = await PostInitializeAsync(host, apiKey: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>An invalid key is refused by the named scheme rather than handed to the default one.</summary>
    [Fact]
    public async Task An_invalid_key_is_refused_without_a_redirect()
    {
        using var host = await BuildHostAsync(o => o.AuthenticationSchemes.Add(KeyScheme));

        using var response = await PostInitializeAsync(host, "wrong-key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpResponseMessage> PostInitializeAsync(IHost host, string apiKey)
    {
        using var content = new StringContent(McpJsonRpcTestClient.InitializeRequest, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (apiKey != null) request.Headers.Add(KeyHeader, apiKey);

        return await host.GetTestClient().SendAsync(request);
    }

    private static async Task<IHost> BuildHostAsync(Action<ThargaMcpOptions> configureOptions = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthentication(InteractiveScheme)
                        .AddScheme<AuthenticationSchemeOptions, RedirectToLoginHandler>(InteractiveScheme, null)
                        .AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(KeyScheme, null);
                    services.AddAuthorization();
                    services.AddThargaMcp(mcp => configureOptions?.Invoke(mcp.Options));
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.UseThargaMcp());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    /// <summary>Stands in for OIDC or cookies: authenticates nobody and answers a challenge with a redirect.</summary>
    private sealed class RedirectToLoginHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status302Found;
            Response.Headers.Location = LoginPage;
            return Task.CompletedTask;
        }
    }

    /// <summary>Stands in for the API-key scheme a bridge package registers.</summary>
    private sealed class ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(KeyHeader, out var provided) || provided != ValidKey)
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "agent")], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
