namespace MetricsHub.Application.Common.Exceptions;

public sealed class DeviceStateStoreException(string message, Exception innerException)
    : Exception(message, innerException);
