namespace Tharga.Mcp.Sample;

internal static class WelcomePage
{
    public const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>Tharga.Mcp.Sample</title>
            <style>
                :root { color-scheme: light dark; }
                * { box-sizing: border-box; }
                body {
                    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                    max-width: 820px;
                    margin: 2rem auto;
                    padding: 0 1.25rem;
                    line-height: 1.55;
                    color: #222;
                    background: #fafafa;
                }
                @media (prefers-color-scheme: dark) {
                    body { color: #e4e4e4; background: #161616; }
                    code, pre { background: #222 !important; color: #f0f0f0 !important; border-color: #333 !important; }
                    .endpoint { background: #1e2a1e !important; border-color: #2d4a2d !important; }
                    a { color: #7cb7ff; }
                    h2 { border-color: #333 !important; }
                }
                h1 { margin-bottom: 0.25rem; }
                .tagline { color: #666; margin-top: 0; }
                h2 { border-bottom: 1px solid #e0e0e0; padding-bottom: 0.35rem; margin-top: 2.25rem; }
                h3 { margin-top: 1.75rem; }
                code {
                    font-family: "SF Mono", Consolas, Menlo, monospace;
                    background: #f1f1f1;
                    padding: 0.12rem 0.35rem;
                    border-radius: 3px;
                    font-size: 0.92em;
                    border: 1px solid #e4e4e4;
                }
                pre {
                    background: #f1f1f1;
                    padding: 0.9rem 1rem;
                    border-radius: 5px;
                    overflow-x: auto;
                    font-size: 0.9em;
                    border: 1px solid #e4e4e4;
                }
                pre code { background: transparent; padding: 0; border: none; }
                .endpoint {
                    display: inline-block;
                    background: #e8f5e8;
                    border: 1px solid #bfddbf;
                    padding: 0.45rem 0.85rem;
                    border-radius: 5px;
                    font-family: "SF Mono", Consolas, Menlo, monospace;
                    font-weight: 600;
                    margin: 0.35rem 0 0.8rem;
                }
                .tools { margin: 0.4rem 0 1rem; }
                .tool { margin-bottom: 0.5rem; }
                .tool-name { font-family: "SF Mono", Consolas, Menlo, monospace; font-weight: 600; }
                ol li, ul li { margin-bottom: 0.35rem; }
                .muted { color: #666; font-size: 0.9em; }
            </style>
        </head>
        <body>
            <h1>Tharga.Mcp.Sample</h1>
            <p class="tagline">Hello-world host for the <a href="https://modelcontextprotocol.io/">Model Context Protocol</a>, built on <code>Tharga.Mcp</code>.</p>

            <h2>MCP endpoint</h2>
            <p>The MCP server is exposed via JSON-RPC over HTTP+SSE at:</p>
            <div class="endpoint">POST /mcp</div>
            <p class="muted">GET/browser requests to <code>/mcp</code> or <code>/</code> aren't meaningful — MCP is a POST-only protocol. Use one of the options below.</p>

            <h2>Available tools</h2>
            <div class="tools">
                <div class="tool">
                    <span class="tool-name">greet(name: string)</span> — Returns a greeting for the given name. <span class="muted">(SDK [McpServerTool] attribute)</span>
                </div>
                <div class="tool">
                    <span class="tool-name">echo(message: string)</span> — Echoes the input back. <span class="muted">(SDK [McpServerTool] attribute)</span>
                </div>
                <div class="tool">
                    <span class="tool-name">time_now()</span> — Returns the current UTC time in ISO-8601 format. <span class="muted">(Tharga IMcpToolProvider)</span>
                </div>
            </div>

            <h2>How to interact</h2>

            <h3>1. MCP Inspector (recommended)</h3>
            <p>Official visual client from the MCP team. Run alongside the sample:</p>
            <pre><code>npx @modelcontextprotocol/inspector</code></pre>
            <p>In the Inspector UI:</p>
            <ul>
                <li>Transport Type: <strong>Streamable HTTP</strong></li>
                <li>URL: <code>http://localhost:5138/mcp</code> <span class="muted">(adjust port if yours differs)</span></li>
                <li>Click <em>Connect</em>, then try <em>List Tools</em> and <em>Call Tool</em>.</li>
            </ul>

            <h3>2. Claude Code</h3>
            <p>Register this sample as a custom MCP server and talk to it directly:</p>
            <pre><code>claude mcp add --transport http tharga-sample http://localhost:5138/mcp</code></pre>
            <p>Then ask Claude Code to call <code>greet</code> or <code>echo</code> — it auto-discovers the tools.</p>

            <h3>3. Raw curl (protocol-level)</h3>
            <p>Useful to prove the plumbing works end-to-end. The MCP streamable HTTP transport is stateful, so capture the session ID from <code>initialize</code> and reuse it:</p>
            <pre><code>SESSION=$(curl -s -D - -X POST http://localhost:5138/mcp \
          -H "Accept: application/json, text/event-stream" \
          -H "Content-Type: application/json" \
          -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}' \
          | grep -i "Mcp-Session-Id" | sed 's/.*: //' | tr -d '\r\n')

        curl -s -X POST http://localhost:5138/mcp \
          -H "Accept: application/json, text/event-stream" \
          -H "Content-Type: application/json" \
          -H "Mcp-Session-Id: $SESSION" \
          -d '{"jsonrpc":"2.0","method":"notifications/initialized"}'

        curl -s -X POST http://localhost:5138/mcp \
          -H "Accept: application/json, text/event-stream" \
          -H "Content-Type: application/json" \
          -H "Mcp-Session-Id: $SESSION" \
          -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"greet","arguments":{"name":"Daniel"}}}'</code></pre>
            <p>Expected last response: <code>Hello, Daniel!</code></p>

            <h2>What this shows</h2>
            <ul>
                <li><code>builder.Services.AddThargaMcp(mcp =&gt; { ... })</code> registers the Tharga MCP foundation.</li>
                <li>Tools are declared via the SDK's <code>[McpServerTool]</code> attribute — see <code>HelloTools.cs</code>.</li>
                <li><code>app.MapMcp()</code> exposes the endpoint at the configured base path (default <code>/mcp</code>).</li>
            </ul>
            <p>See the <a href="https://github.com/Tharga/Mcp">Tharga.Mcp</a> README for the full API.</p>
        </body>
        </html>
        """;
}
