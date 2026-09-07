using MetricsHub.Application.DeviceStates;

namespace MetricsHub.Application.Abstractions.Persistence;

public interface IDeviceStateStore
{
    Task<DeviceState?> GetAsync(Guid deviceId, CancellationToken cancellationToken);

    Task SetAsync(DeviceState state, CancellationToken cancellationToken);

    Task RemoveAsync(Guid deviceId, CancellationToken cancellationToken);
}
