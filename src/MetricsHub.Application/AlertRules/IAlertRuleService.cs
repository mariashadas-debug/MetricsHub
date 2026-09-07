namespace MetricsHub.Application.AlertRules;

public interface IAlertRuleService
{
    Task<IReadOnlyList<AlertRuleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<AlertRuleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertRuleResponse> CreateAsync(SaveAlertRuleCommand command, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, SaveAlertRuleCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
