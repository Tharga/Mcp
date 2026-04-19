using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tharga.Mcp.Tests.Bridge;

/// <summary>
/// Minimal raw JSON-RPC MCP client for end-to-end tests. Handles the initialize handshake, captures the Mcp-Session-Id,
/// and exposes Send for subsequent requests. Responses are parsed from the SDK's text/event-stream body.
/// </summary>
internal sealed class McpJsonRpcTestClient : IDisposable
{
    private readonly HttpClient _http;
    private string _sessionId;
    private int _nextId = 2;

    public McpJsonRpcTestClient(HttpClient http)
    {
        _http = http;
    }

    public async Task InitializeAsync()
    {
        var body = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}""";
        using var response = await PostRawAsync(body);
        response.EnsureSuccessStatusCode();
        _sessionId = response.Headers.GetValues("Mcp-Session-Id").Single();

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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = content };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        if (_sessionId != null) request.Headers.Add("Mcp-Session-Id", _sessionId);

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
