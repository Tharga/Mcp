# Plan: provider-bridge

Branch: `feature/provider-bridge`
Feature: see `plan/feature.md`

## Steps

### 1. Investigate SDK handler API [x]
Handlers live on `McpServerOptions.Handlers` (type `McpServerHandlers`), not on `Capabilities.*` as I initially assumed. Signature:

```csharp
public delegate ValueTask<TResult> McpRequestHandler<TParams, TResult>(
    RequestContext<TParams> request, CancellationToken cancellationToken);

McpRequestHandler<ListToolsRequestParams, ListToolsResult>?     ListToolsHandler
McpRequestHandler<CallToolRequestParams, CallToolResult>?       CallToolHandler
McpRequestHandler<ListResourcesRequestParams, ListResourcesResult>? ListResourcesHandler
McpRequestHandler<ReadResourceRequestParams, ReadResourceResult>?   ReadResourceHandler
```

**Coexistence is free** (from SDK XML docs):
- `ListToolsHandler`: "works alongside any tools defined in the McpServerTool collection. Tools from both sources will be combined"
- `CallToolHandler`: "invoked when a client makes a call to a tool that isn't found in the McpServerTool collection"

So attribute-based `[McpServerTool]` tools take priority; our handler runs only for tool names they don't claim. That's exactly what we want.

**SDK types we'll map to/from:**
- `Tool { Name (required), Title, Description, InputSchema (JsonElement, must pass MCP schema validation) }`
- `Resource { Name (required), Uri (required), Title, Description, MimeType, Annotations, Size }`
- `CallToolRequestParams { Name (required), Arguments: IDictionary<string, JsonElement>? }`
- `CallToolResult { Content: IList<ContentBlock>, IsError? }`
- `ReadResourceRequestParams { Uri (required) }`
- `ReadResourceResult { Contents: IList<ResourceContents> }`
- `TextContentBlock : ContentBlock { Type = "text", Text (required) }`
- `TextResourceContents : ResourceContents { Uri, MimeType, Text (required) }`
- `BlobResourceContents : ResourceContents { FromBytes(ReadOnlyMemory<byte>, uri, mimeType) }`
- Lists inherit from `PaginatedResult`; pagination deferred for Phase 0 (return everything in one page — consistent with how attribute-based tools behave).

**Configuration path:** `IConfigureOptions<McpServerOptions>` implementation that populates `options.Handlers.*`. Gives us DI access for resolving providers per-request. The handler can also read from `RequestContext<T>.Services` for per-request DI if needed.

### 2. Types bridge [x]
Added `Internal/McpTypeMappers.cs` with:
- `ToSdkTool(McpToolDescriptor)` — falls back to `{"type":"object"}` schema when our `InputSchema` is null (SDK requires a valid schema)
- `ToSdkResource(McpResourceDescriptor)`
- `ToSdkCallToolResult(McpToolResult)` — maps `McpContent` list → `ContentBlock` list (text-only for Phase 0)
- `ToSdkReadResourceResult(McpResourceContent)` — returns text or blob variant based on which field is populated
- `ArgumentsToJsonElement(IDictionary<string, JsonElement>)` — packs the SDK's flat dict into a JsonElement object so our `IMcpToolProvider.CallToolAsync(JsonElement arguments)` receives a single root-object element

Build clean.
Build mappers between Tharga descriptors and SDK types:
- `McpToolDescriptor` ↔ SDK `Tool`
- `McpResourceDescriptor` ↔ SDK `Resource`
- `McpToolResult` ↔ SDK `CallToolResponse`
- `McpResourceContent` ↔ SDK `ResourceContents` (text / blob)
- `McpContent` ↔ SDK content blocks

Internal static `McpTypeMappers` with helpers for each direction.

### 3. McpProviderDispatcher [x]
Singleton `Internal/McpProviderDispatcher` implementing the four handler methods. Each:
- Reads `IMcpContextAccessor.Current` from per-request `request.Services`
- Enumerates `IEnumerable<IMcpResourceProvider>` / `IEnumerable<IMcpToolProvider>` from DI
- **Scope filtering:** if `Current == null` → no filter (Phase 0 default); if set → filter providers by matching `Scope`
- **Fallback context** when `Current` is null: `Scope = System`, `IsDeveloper = true`, other fields null. Means in Phase 0 the dispatcher passes a well-defined context to providers even before the Platform bridge lands.
- **Tool dispatch:** iterates providers, asks each for `ListToolsAsync`, delegates to the first one whose list advertises the requested name. Unknown tool → `CallToolResult { IsError = true, Content = [text "Unknown tool: {name}"] }`.
- **Resource dispatch:** same pattern; unknown URI → throws `InvalidOperationException` (SDK converts to JSON-RPC error).

Build clean. Tests for the dispatcher land in step 5 via the end-to-end TestServer flow.
Under `Tharga.Mcp/Internal/`:
- Takes `IServiceProvider`, `IMcpContextAccessor`, and the SDK's existing handlers (if any) as inputs
- Implements the four handler callbacks
- On each call:
  1. Read `IMcpContextAccessor.Current` → effective `McpScope?` (null = no filter)
  2. Enumerate `IEnumerable<IMcpToolProvider>` / `IEnumerable<IMcpResourceProvider>` from DI
  3. Filter by scope (if effective scope not null)
  4. For ListTools/ListResources: aggregate, merging with SDK-attribute-based results if preserved
  5. For CallTool: find owning provider (by tool name), delegate
  6. For ReadResource: find owning provider (by URI match — first provider whose `ListResourcesAsync` advertised the URI)
- Build a per-call `IMcpContext` if `Current` is null, populate the scope field so provider methods see a consistent context

### 4. Wire up in AddThargaMcp [~]
In `ThargaMcpServiceCollectionExtensions.AddThargaMcp`:
- Register the dispatcher
- Configure `McpServerOptions` via `services.Configure<McpServerOptions>(...)` or the SDK's recommended pattern so the dispatcher's handlers land on the final options
- Keep attribute-based tool registration working — investigate whether SDK stacks them or we need to explicitly chain

### 5. Tests [ ]
New tests covering:
- Tool provider end-to-end: register `FakeToolProvider`, POST initialize → tools/list → tools/call, verify round-trip
- Resource provider end-to-end: same pattern
- Scope filter: register two tool providers with different scopes, set `IMcpContextAccessor.Current` to one scope, verify only that provider's tools are listed
- No-context fallback: leave `Current` null, verify all scopes visible
- Coexistence: attribute-based tool + provider-based tool in the same server, both discoverable
- Unknown tool name: `tools/call` with a name no provider owns → returns `-32602` invalid-params or the SDK's standard unknown-tool error

### 6. Sample update [ ]
Add one `IMcpToolProvider` to `Sample/Tharga.Mcp.Sample/` (e.g. `TimeToolProvider` with a single `time.now` tool). Register it via `mcp.AddToolProvider<TimeToolProvider>()`. Leave `HelloTools` as-is so the sample shows both paths.

Update `WelcomePage.Html` tool list to mention the new tool.

### 7. README update [ ]
Replace the "Provider contracts" section placeholder text with a real example:

```csharp
public sealed class TimeToolProvider : IMcpToolProvider { ... }

builder.Services.AddThargaMcp(mcp =>
{
    mcp.AddToolProvider<TimeToolProvider>();
});
```

Add a note about scope-based filtering coming with Phase 1.

### 8. Close the Requests.md entry [ ]
Per shared-instructions "Feature Requests (cross-project)":
1. Mark the request Done in `$DOC_ROOT/Tharga/Requests.md` with completion date + summary
2. Add Follow-up item: "Tharga.MongoDB.Mcp should upgrade Tharga.Mcp to X.Y.Z — IMcpToolProvider/IMcpResourceProvider bridge"

## Commit milestones

- After step 2: "feat: SDK-type mappers"
- After step 3: "feat: McpProviderDispatcher"
- After step 4: "feat: wire provider dispatcher into AddThargaMcp"
- After step 5: "test: provider-bridge end-to-end coverage"
- After step 6: "feat: sample demonstrates provider-based tool"
- After step 7: "docs: README provider-style example"
- After step 8: "chore: close provider-bridge request"

Run `dotnet build -c Release` and `dotnet test -c Release` before each commit. Never commit failing tests.

## Progress log

_(updated during implementation)_

## Last session

_(updated at end of session)_
