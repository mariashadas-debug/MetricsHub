using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Domain.Entities;

namespace MetricsHub.Application.AlertRules;

internal sealed class AlertRuleService(IAlertRepository alertRepository, IDeviceRepository deviceRepository) : IAlertRuleService
{
    public async Task<IReadOnlyList<AlertRuleResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await alertRepository.GetRulesAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<AlertRuleResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await alertRepository.GetRuleAsync(id, false, cancellationToken)
            ?? throw new NotFoundException($"Alert rule '{id}' was not found."));

    public async Task<AlertRuleResponse> CreateAsync(SaveAlertRuleCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var device = await GetDevice(command.DeviceId, cancellationToken);
        var rule = new AlertRule(command.Name, command.MetricType, command.Operator, command.Threshold, command.Severity, device);
        if (!command.IsEnabled)
        {
            rule.Update(command.Name, command.MetricType, command.Operator, command.Threshold, command.Severity, false, device);
        }
        alertRepository.AddRule(rule);
        await alertRepository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task UpdateAsync(Guid id, SaveAlertRuleCommand command, CancellationToken cancellationToken)
    {
        Validate(command);
        var rule = await alertRepository.GetRuleAsync(id, true, cancellationToken)
            ?? throw new NotFoundException($"Alert rule '{id}' was not found.");
        var device = await GetDevice(command.DeviceId, cancellationToken);
        rule.Update(command.Name, command.MetricType, command.Operator, command.Threshold, command.Severity, command.IsEnabled, device);
        await alertRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await alertRepository.GetRuleAsync(id, true, cancellationToken)
            ?? throw new NotFoundException($"Alert rule '{id}' was not found.");
        alertRepository.RemoveRule(rule);
        await alertRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Device?> GetDevice(Guid? deviceId, CancellationToken cancellationToken)
    {
        if (!deviceId.HasValue) return null;
        return await deviceRepository.GetByIdAsync(deviceId.Value, true, cancellationToken)
            ?? throw new NotFoundException($"Device '{deviceId}' was not found.");
    }

    private static void Validate(SaveAlertRuleCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) throw new ApplicationValidationException("Name is required.");
        if (command.Name.Length > 200) throw new ApplicationValidationException("Name cannot exceed 200 characters.");
        if (!double.IsFinite(command.Threshold)) throw new ApplicationValidationException("Threshold must be a finite number.");
    }

    private static AlertRuleResponse Map(AlertRule rule) => new(rule.Id, rule.DeviceId, rule.Name, rule.MetricType, rule.Operator, rule.Threshold, rule.Severity, rule.IsEnabled);
}
