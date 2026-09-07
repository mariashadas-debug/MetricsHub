using MetricsHub.Api.Contracts.AlertRules;
using MetricsHub.Application.AlertRules;
using Microsoft.AspNetCore.Mvc;

namespace MetricsHub.Api.Controllers;

[ApiController]
[Route("api/v1/alert-rules")]
public sealed class AlertRulesController(IAlertRuleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertRuleResponse>>> GetAll(CancellationToken cancellationToken) => Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<ActionResult<AlertRuleResponse>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<AlertRuleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AlertRuleResponse>> Create(SaveAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(Map(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, SaveAlertRuleRequest request, CancellationToken cancellationToken)
    {
        await service.UpdateAsync(id, Map(request), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static SaveAlertRuleCommand Map(SaveAlertRuleRequest request) => new(request.DeviceId, request.Name, request.MetricType!.Value, request.Operator!.Value, request.Threshold, request.Severity!.Value, request.IsEnabled);
}
