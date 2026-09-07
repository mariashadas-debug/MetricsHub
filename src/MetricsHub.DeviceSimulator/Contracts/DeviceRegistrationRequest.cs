namespace MetricsHub.DeviceSimulator.Contracts;

public sealed record DeviceRegistrationRequest(
    string DeviceKey,
    string Name,
    string Type,
    string Hostname,
    string OperatingSystem,
    string Location);
