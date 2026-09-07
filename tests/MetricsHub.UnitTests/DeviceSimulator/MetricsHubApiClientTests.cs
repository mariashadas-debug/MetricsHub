using System.Net;
using System.Text;
using MetricsHub.DeviceSimulator.Client;
using MetricsHub.DeviceSimulator.Configuration;
using MetricsHub.DeviceSimulator.Simulation;

namespace MetricsHub.UnitTests.DeviceSimulator;

public sealed class MetricsHubApiClientTests
{
    [Fact]
    public async Task EnsureDeviceRegistered_WhenMissing_UsesDeviceEndpointAndStringType()
    {
        var requests = new List<(HttpMethod Method, string Path, string Body)>();
        using var client = CreateClient(async request =>
        {
            requests.Add((
                request.Method,
                request.RequestUri!.AbsolutePath,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync()));

            return request.Method == HttpMethod.Get
                ? JsonResponse("[]")
                : JsonResponse("{}", HttpStatusCode.Created);
        });

        var result = await client.EnsureDeviceRegisteredAsync(CreateDevice(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(requests,
            request => Assert.Equal((HttpMethod.Get, "/api/v1/devices"), (request.Method, request.Path)),
            request =>
            {
                Assert.Equal((HttpMethod.Post, "/api/v1/devices"), (request.Method, request.Path));
                Assert.Contains("\"type\":\"Workstation\"", request.Body);
            });
    }

    [Fact]
    public async Task EnsureDeviceRegistered_WhenCreateConflicts_TreatsDeviceAsReady()
    {
        using var client = CreateClient(request => Task.FromResult(
            request.Method == HttpMethod.Get
                ? JsonResponse("[]")
                : JsonResponse("{}", HttpStatusCode.Conflict)));

        var result = await client.EnsureDeviceRegisteredAsync(CreateDevice(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task SendTelemetry_UsesTelemetryEndpointAndUtcPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        using var client = CreateClient(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var timestamp = new DateTime(2026, 9, 7, 18, 30, 0, DateTimeKind.Utc);
        var snapshot = new TelemetrySnapshot(timestamp,
            [new MetricSample("CpuUsage", 43.8, "%")]);

        var result = await client.SendTelemetryAsync("windows-dev-01", snapshot, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/telemetry", capturedRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("2026-09-07T18:30:00Z", capturedBody);
        Assert.Contains("\"type\":\"CpuUsage\"", capturedBody);
    }

    private static MetricsHubApiClient CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://localhost:7091/")
        };

        return new MetricsHubApiClient(httpClient);
    }

    private static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static SimulatedDeviceOptions CreateDevice() => new()
    {
        DeviceKey = "windows-dev-01",
        Name = "Development Workstation",
        Type = "Workstation",
        Hostname = "DEV-PC-01",
        OperatingSystem = "Windows 11",
        Location = "Development Office"
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request);
    }
}
