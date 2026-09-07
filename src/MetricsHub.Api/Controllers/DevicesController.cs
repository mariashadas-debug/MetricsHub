using MetricsHub.Api.Contracts.Devices;
using MetricsHub.Application.Devices;
using Microsoft.AspNetCore.Mvc;

namespace MetricsHub.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
public sealed class DevicesController(IDeviceService deviceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DeviceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await deviceService.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    [ProducesResponseType<DeviceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await deviceService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<DeviceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> Create(
        CreateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await deviceService.CreateAsync(new CreateDeviceCommand(
            request.DeviceKey,
            request.Name,
            request.Type!.Value,
            request.Hostname,
            request.OperatingSystem,
            request.Location), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        await deviceService.UpdateAsync(id, new UpdateDeviceCommand(
            request.Name,
            request.Type!.Value,
            request.Hostname,
            request.OperatingSystem,
            request.Location,
            request.IsEnabled!.Value), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deviceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
