using System.Net;
using System.Net.Http.Json;
using MetricsHub.DeviceSimulator.Configuration;
using MetricsHub.DeviceSimulator.Contracts;
using MetricsHub.DeviceSimulator.Simulation;

namespace MetricsHub.DeviceSimulator.Client;

public sealed class MetricsHubApiClient(HttpClient httpClient) : IMetricsHubApiClient, IDisposable
{
    public async Task<ApiCallResult> EnsureDeviceRegisteredAsync(
        SimulatedDeviceOptions device,
        CancellationToken cancellationToken)
    {
        try
        {
            using var listResponse = await httpClient.GetAsync("api/v1/devices", cancellationToken);

            if (!listResponse.IsSuccessStatusCode)
            {
                return ApiCallResult.Failure(
                    listResponse.StatusCode,
                    "Could not query registered devices.");
            }

            var devices = await listResponse.Content.ReadFromJsonAsync<DeviceSummaryResponse[]>(
                cancellationToken: cancellationToken) ?? [];

            if (devices.Any(existing =>
                    string.Equals(existing.DeviceKey, device.DeviceKey, StringComparison.Ordinal)))
            {
                return ApiCallResult.Success(HttpStatusCode.OK);
            }

            using var createResponse = await httpClient.PostAsJsonAsync(
                "api/v1/devices",
                new DeviceRegistrationRequest(
                    device.DeviceKey,
                    device.Name,
                    device.Type,
                    device.Hostname,
                    device.OperatingSystem,
                    device.Location),
                cancellationToken);

            if (createResponse.IsSuccessStatusCode || createResponse.StatusCode == HttpStatusCode.Conflict)
            {
                return ApiCallResult.Success(createResponse.StatusCode);
            }

            return ApiCallResult.Failure(
                createResponse.StatusCode,
                "Device registration was rejected.");
        }
        catch (HttpRequestException exception)
        {
            return ApiCallResult.Failure(exception.StatusCode, exception.Message);
        }
    }

    public async Task<ApiCallResult> SendTelemetryAsync(
        string deviceKey,
        TelemetrySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/v1/telemetry",
                new TelemetryRequest(
                    deviceKey,
                    snapshot.Timestamp,
                    snapshot.Metrics.Select(metric => new TelemetryMetricRequest(
                        metric.Type,
                        metric.Value,
                        metric.Unit)).ToArray()),
                cancellationToken);

            return response.IsSuccessStatusCode
                ? ApiCallResult.Success(response.StatusCode)
                : ApiCallResult.Failure(response.StatusCode, "Telemetry was rejected.");
        }
        catch (HttpRequestException exception)
        {
            return ApiCallResult.Failure(exception.StatusCode, exception.Message);
        }
    }

    public void Dispose() => httpClient.Dispose();
}
