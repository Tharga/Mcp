using Tharga.Mcp;
using Tharga.Mcp.Sample;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddThargaMcp(mcp =>
{
    mcp.Services.AddMcpServer().WithTools<HelloTools>();
});

var app = builder.Build();

app.MapMcp();

app.Run();
