using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MetricsHub.Application.Devices;

internal sealed class DeviceService(
    IDeviceRepository repository,
    IDeviceStateStore deviceStateStore,
    ILogger<DeviceService> logger) : IDeviceService
{
    public async Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var devices = await repository.GetAllAsync(cancellationToken);
        return devices.Select(Map).ToArray();
    }

    public async Task<DeviceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(id, false, cancellationToken)
            ?? throw new NotFoundException($"Device '{id}' was not found.");

        return Map(device);
    }

    public async Task<DeviceResponse> CreateAsync(
        CreateDeviceCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRequired(command.DeviceKey, "DeviceKey");
        ValidateRequired(command.Name, "Name");

        if (await repository.GetByKeyAsync(command.DeviceKey, false, cancellationToken) is not null)
        {
            throw new ConflictException($"DeviceKey '{command.DeviceKey}' is already registered.");
        }

        var device = new Device(
            command.Name,
            command.DeviceKey,
            command.Type,
            command.Hostname,
            command.OperatingSystem,
            command.Location);

        repository.Add(device);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(device);
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateDeviceCommand command,
        CancellationToken cancellationToken)
    {
        ValidateRequired(command.Name, "Name");

        var device = await repository.GetByIdAsync(id, true, cancellationToken)
            ?? throw new NotFoundException($"Device '{id}' was not found.");

        device.UpdateDetails(
            command.Name,
            command.Type,
            command.Hostname,
            command.OperatingSystem,
            command.Location,
            command.IsEnabled);

        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var device = await repository.GetByIdAsync(id, true, cancellationToken)
            ?? throw new NotFoundException($"Device '{id}' was not found.");

        if (await repository.HasHistoricalDataAsync(id, cancellationToken))
        {
            throw new ConflictException(
                "The device cannot be deleted because historical telemetry or alerts reference it.");
        }

        repository.Remove(device);
        await repository.SaveChangesAsync(cancellationToken);

        try
        {
            await deviceStateStore.RemoveAsync(id, cancellationToken);
        }
        catch (DeviceStateStoreException exception)
        {
            logger.LogWarning(exception, "Redis state removal failed: DeviceId={DeviceId}", id);
        }
    }

    private static DeviceResponse Map(Device device) => new(
        device.Id,
        device.DeviceKey,
        device.Name,
        device.Type,
        device.Status,
        device.Hostname,
        device.OperatingSystem,
        device.Location,
        device.IsEnabled,
        device.CreatedAt,
        device.LastSeenAt);

    private static void ValidateRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationValidationException($"{fieldName} is required.");
        }
    }
}
