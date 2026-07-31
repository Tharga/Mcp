using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tharga.Mcp.Tests.Bridge;

/// <summary>
/// Minimal raw JSON-RPC MCP client for end-to-end tests. Handles the initialize handshake, captures the Mcp-Session-Id
/// when the server issues one, and exposes Send for subsequent requests. Responses are parsed from the SDK's
/// text/event-stream body.
/// </summary>
/// <remarks>
/// A session is optional: ModelContextProtocol 2.0.0 made <c>HttpServerTransportOptions.Stateless</c> default to true,
/// and a stateless server creates no session and returns no <c>Mcp-Session-Id</c>. Subsequent requests then carry no
/// session header, which is what the server expects.
/// </remarks>
internal sealed class McpJsonRpcTestClient : IDisposable
{
    /// <summary>The initialize envelope, shared with tests that post it raw to observe the response themselves.</summary>
    internal const string InitializeRequest = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}""";

    private const string SessionHeader = "Mcp-Session-Id";

    private readonly HttpClient _http;
    private string _sessionId;
    private int _nextId = 2;

    public McpJsonRpcTestClient(HttpClient http)
    {
        _http = http;
    }

    public async Task InitializeAsync()
    {
        using var response = await PostRawAsync(InitializeRequest);
        response.EnsureSuccessStatusCode();
        _sessionId = response.Headers.TryGetValues(SessionHeader, out var sessionIds) ? sessionIds.Single() : null;

        using var notify = await PostRawAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
        notify.EnsureSuccessStatusCode();
    }

    public async Task<JsonElement> SendAsync(string method, object parameters = null)
    {
        var id = _nextId++;
        var payload = new Dictionary<string, object>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
        };
        if (parameters != null) payload["params"] = parameters;

        using var response = await PostRawAsync(JsonSerializer.Serialize(payload));
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        return ParseEventStream(responseBody);
    }

    private async Task<HttpResponseMessage> PostRawAsync(string jsonBody)
    {
        using var content = new StringContent(jsonBody, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        request.Content = content;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (_sessionId != null) request.Headers.Add(SessionHeader, _sessionId);

        return await _http.SendAsync(request);
    }

    private static JsonElement ParseEventStream(string body)
    {
        foreach (var line in body.Split('\n'))
        {
            if (line.StartsWith("data: "))
            {
                return JsonDocument.Parse(line[6..]).RootElement.Clone();
            }
        }
        throw new InvalidOperationException($"No data line in SSE response: {body}");
    }

    public void Dispose() => _http.Dispose();
}
