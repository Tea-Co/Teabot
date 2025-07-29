using Teabot.Agent;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ✦ Set up Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ✦ Set up services
builder.Services.AddSingleton<ToolRegistry>();
builder.Services.AddSingleton<LlmAgent>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✦ Build the app
var app = builder.Build();

// ✦ Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✦ Set up routes
app.MapGet("/health", () => Results.Ok("Healthy"));

app.MapPost("/tools/register", async (ToolRegistration registration, ToolRegistry registry) =>
{
    var tool = new Tool(
        registration.Name,
        registration.Description,
        registration.Parameters,
        registration.Required,
        registration.Endpoint
    );

    registry.Register(tool);
    return Results.Ok("Tool registered successfully.");
});

app.MapGet("/tools/describe", (ToolRegistry registry) => registry.DescribeTools);

app.MapPost("/agent/execute", async ([FromBody] string prompt, LlmAgent agent) =>
{
    Log.Information(prompt);

    await agent.RunAsync(prompt);

    Log.Information("Agent executed successfully.");
});

// ✦ Run the app
app.Run("http://0.0.0.0");
