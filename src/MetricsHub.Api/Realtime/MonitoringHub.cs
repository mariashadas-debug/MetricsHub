using Microsoft.AspNetCore.SignalR;

namespace MetricsHub.Api.Realtime;

public sealed class MonitoringHub : Hub<IMonitoringClient>
{
    private const string GlobalGroup = "monitoring:all";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroup);
        await base.OnConnectedAsync();
    }

    public async Task SubscribeToDevice(Guid deviceId)
    {
        if (deviceId == Guid.Empty) throw new HubException("A valid deviceId is required.");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GlobalGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));
    }

    public async Task UnsubscribeFromDevice(Guid deviceId)
    {
        if (deviceId == Guid.Empty) throw new HubException("A valid deviceId is required.");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));
        await Groups.AddToGroupAsync(Context.ConnectionId, GlobalGroup);
    }

    internal static string DeviceGroup(Guid deviceId) => $"device:{deviceId:D}";
    internal static string AllDevicesGroup => GlobalGroup;
}
