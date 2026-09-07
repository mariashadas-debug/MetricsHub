namespace MetricsHub.Application.Devices;

public interface IDeviceService
{
    Task<IReadOnlyList<DeviceResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<DeviceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<DeviceResponse> CreateAsync(CreateDeviceCommand command, CancellationToken cancellationToken);

    Task UpdateAsync(Guid id, UpdateDeviceCommand command, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
