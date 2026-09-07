using MetricsHub.Domain.Entities;

namespace MetricsHub.Application.Abstractions.Persistence;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken);

    Task<Device?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken);

    Task<Device?> GetByKeyAsync(string deviceKey, bool trackChanges, CancellationToken cancellationToken);

    Task<bool> HasHistoricalDataAsync(Guid id, CancellationToken cancellationToken);

    void Add(Device device);

    void Remove(Device device);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
