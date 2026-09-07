using System.Text.Json.Serialization;
using MetricsHub.Api.ErrorHandling;
using MetricsHub.Application;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
    {
        status = "Healthy",
        message = "MetricsHub API is running."
    }))
    .WithName("GetHealth")
    .WithTags("Health");

app.Run();

public partial class Program;
