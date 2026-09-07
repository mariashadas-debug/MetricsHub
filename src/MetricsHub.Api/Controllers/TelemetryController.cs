using MetricsHub.Api.Contracts.Telemetry;
using MetricsHub.Application.Telemetry;
using MetricsHub.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MetricsHub.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class TelemetryController(ITelemetryService telemetryService) : ControllerBase
{
    [HttpPost("telemetry")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ingest(
        IngestTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        await telemetryService.IngestAsync(new IngestTelemetryCommand(
            request.DeviceKey,
            request.Timestamp!.Value,
            request.Metrics.Select(metric => new TelemetryMetricInput(
                metric.Type!.Value,
                metric.Value,
                metric.Unit)).ToArray()), cancellationToken);

        return NoContent();
    }

    [HttpGet("devices/{deviceId}/telemetry")]
    [ProducesResponseType<IReadOnlyList<TelemetryPointResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TelemetryPointResponse>>> GetHistory(
        Guid deviceId,
        [FromQuery] MetricType? metricType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 500,
        CancellationToken cancellationToken = default) =>
        Ok(await telemetryService.GetHistoryAsync(
            deviceId,
            new TelemetryHistoryQuery(metricType, from, to, limit),
            cancellationToken));

    [HttpGet("devices/{deviceId}/telemetry/latest")]
    [ProducesResponseType<LatestTelemetryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LatestTelemetryResponse>> GetLatest(
        Guid deviceId,
        CancellationToken cancellationToken) =>
        Ok(await telemetryService.GetLatestAsync(deviceId, cancellationToken));
}
