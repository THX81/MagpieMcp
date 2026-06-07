using MagpieMcp.Api;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

// Enable CORS Middleware
// IMPORTANT: This must be placed BEFORE MapMcp() and any other endpoints
app.UseCors();

app.MapMcp(); // Maps the SSE endpoint

app.MapGet("/", () => "Dynamic MCP Server Running");

app.Run();
