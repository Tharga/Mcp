using Tharga.Mcp;
using Tharga.Mcp.Sample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThargaMcp(mcp =>
{
    // Attribute-based tools via the SDK's pattern
    mcp.Services.AddMcpServer().WithTools<HelloTools>();

    // Provider-based tools via the Tharga contract — both paths coexist
    mcp.AddToolProvider<TimeToolProvider>();
});

var app = builder.Build();

app.MapGet("/", () => Results.Content(WelcomePage.Html, "text/html; charset=utf-8"));
app.UseThargaMcp();

app.Run();
