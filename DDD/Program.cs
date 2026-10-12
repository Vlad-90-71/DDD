using DDD.Application.Extensions;
using DDD.Eventing.Contracts;
using DDD.Extensions;
using DDD.Infrastructure;
using DDD.Infrastructure.Extensions;
using DDD.Infrastructure.Monitoring;
using DDD.MessageBroker.InMemory;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

builder.Services.AddPresentationControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddInMemoryMessageBroker();

builder.Services.AddSingleton<IDomainEventMetrics, DddMetrics>();

builder.Services
    .AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(DddMetrics.MeterName)
        .AddPrometheusExporter());

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: ["live"])
    .AddDbContextCheck<AppDbContext>(
        name: "database",
        tags: ["ready"]);

var app = builder.Build();

// Самый первый
app.UseExceptionHandler();

app.UseSerilogRequestLogging();

app.MapPrometheusScrapingEndpoint();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

