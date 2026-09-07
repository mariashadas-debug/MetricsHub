using MetricsHub.Application.Abstractions.Persistence;
using MetricsHub.Application.Common.Exceptions;
using MetricsHub.Domain.Entities;
using MetricsHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace MetricsHub.Infrastructure.Persistence;

internal sealed class DeviceRepository(MetricsHubDbContext context) : IDeviceRepository
{
    public async Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken) =>
        await context.Devices
            .AsNoTracking()
            .OrderBy(device => device.Name)
            .ToListAsync(cancellationToken);

    public Task<Device?> GetByIdAsync(
        Guid id,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = trackChanges ? context.Devices : context.Devices.AsNoTracking();
        return query.SingleOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public Task<Device?> GetByKeyAsync(
        string deviceKey,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        var query = trackChanges ? context.Devices : context.Devices.AsNoTracking();
        return query.SingleOrDefaultAsync(device => device.DeviceKey == deviceKey, cancellationToken);
    }

    public async Task<bool> HasHistoricalDataAsync(Guid id, CancellationToken cancellationToken) =>
        await context.TelemetryPoints.AnyAsync(point => point.DeviceId == id, cancellationToken)
        || await context.Alerts.AnyAsync(alert => alert.DeviceId == id, cancellationToken);

    public void Add(Device device) => context.Devices.Add(device);

    public void Remove(Device device) => context.Devices.Remove(device);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (FindMySqlException(exception)?.Number == 1062)
        {
            throw new ConflictException("The DeviceKey is already registered.");
        }
        catch (DbUpdateException exception) when (FindMySqlException(exception)?.Number == 1451)
        {
            throw new ConflictException(
                "The device cannot be deleted because historical data references it.");
        }
    }

    private static MySqlException? FindMySqlException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is MySqlException mySqlException)
            {
                return mySqlException;
            }
        }

        return null;
    }
}
