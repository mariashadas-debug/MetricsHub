using System.Text.Json.Serialization;
using MetricsHub.Api.ErrorHandling;
using MetricsHub.Api.BackgroundServices;
using MetricsHub.Api.Realtime;
using MetricsHub.Application;
using MetricsHub.Application.Abstractions.Realtime;
using MetricsHub.Application.Monitoring;
using MetricsHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddOptions<MonitoringOptions>()
    .Bind(builder.Configuration.GetSection(MonitoringOptions.SectionName))
    .Validate(options => options.OfflineAfterSeconds > 0, "Monitoring:OfflineAfterSeconds must be greater than zero.")
    .Validate(options => options.OfflineCheckIntervalSeconds > 0, "Monitoring:OfflineCheckIntervalSeconds must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddHostedService<DeviceOfflineMonitor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapControllers();
app.MapHub<MonitoringHub>("/hubs/monitoring");

app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        message = "MetricsHub API is running."
    }))
    .WithName("GetHealth")
    .WithTags("Health");

app.Run();

public partial class Program;
