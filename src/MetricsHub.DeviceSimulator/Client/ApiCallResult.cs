using System.Net;

namespace MetricsHub.DeviceSimulator.Client;

public sealed record ApiCallResult(bool IsSuccess, HttpStatusCode? StatusCode, string? Error)
{
    public static ApiCallResult Success(HttpStatusCode statusCode) => new(true, statusCode, null);

    public static ApiCallResult Failure(HttpStatusCode? statusCode, string error) =>
        new(false, statusCode, error);
}
