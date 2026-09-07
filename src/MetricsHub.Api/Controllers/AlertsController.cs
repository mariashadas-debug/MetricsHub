using MetricsHub.Application.Alerts;
using MetricsHub.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MetricsHub.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AlertsController(IAlertService service) : ControllerBase
{
    [HttpGet("alerts")]
    [ProducesResponseType<IReadOnlyList<AlertResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetAll([FromQuery] bool? isResolved, [FromQuery] AlertSeverity? severity, [FromQuery] Guid? deviceId, [FromQuery] int limit = 200, CancellationToken cancellationToken = default) =>
        Ok(await service.GetAllAsync(new AlertQuery(isResolved, severity, deviceId, limit), cancellationToken));

    [HttpGet("alerts/{id}")]
    [ProducesResponseType<AlertResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await service.GetByIdAsync(id, cancellationToken));

    [HttpGet("devices/{deviceId}/alerts")]
    [ProducesResponseType<IReadOnlyList<AlertResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> GetForDevice(Guid deviceId, [FromQuery] int limit = 200, CancellationToken cancellationToken = default) =>
        Ok(await service.GetForDeviceAsync(deviceId, limit, cancellationToken));
}
