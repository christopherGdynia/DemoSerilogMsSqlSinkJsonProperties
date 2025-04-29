using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog;
using Serilog.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(
    delegate(HostBuilderContext context, IServiceProvider _, LoggerConfiguration configuration)
    {
        configuration.ReadFrom.Configuration(context.Configuration);
    }
);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSerilogRequestLogging(
    delegate(RequestLoggingOptions options)
    {
        options.EnrichDiagnosticContext = delegate(
            IDiagnosticContext diagnosticContext,
            HttpContext httpContext
        )
        {
            Endpoint? endpoint = httpContext.GetEndpoint();
            if (endpoint is null)
                return;
            ControllerActionDescriptor? metadata =
                endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (metadata is null)
                return;

            string? value = metadata.AttributeRouteInfo?.Template;
            if (value is null)
                return;
            diagnosticContext.Set("RouteTemplate", value);
        };
    }
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "DemoSerilogMsSqlSinkJsonPropertiesSolution v1"
        );
        c.RoutePrefix = ""; // <-- This serves Swagger UI at "/"
    });
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching",
};

app.MapGet(
        "/weatherforecast",
        () =>
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            return forecast;
        }
    )
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.MapGet(
        "/error",
        () =>
        {
            return Results.Problem("Synthetic Error", statusCode: 500);
        }
    )
    .WithName("Error")
    .WithOpenApi();

#if DEBUG
Console.WriteLine("Now listening on: https://localhost:5005");
#endif

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
