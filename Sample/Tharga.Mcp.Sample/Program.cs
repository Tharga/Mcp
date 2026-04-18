var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Tharga.Mcp sample — endpoints come in step 5.");

app.Run();
