namespace Device.Contracts.Common;

public class DeviceResult
{
    public bool Success { get; init; }

    public DeviceErrorCode ErrorCode { get; init; } = DeviceErrorCode.None;

    public string? Message { get; init; }

    public string? DeviceId { get; init; }

    public string? RequestId { get; init; }

    public static DeviceResult Ok(string? deviceId = null, string? requestId = null) =>
        new()
        {
            Success = true,
            ErrorCode = DeviceErrorCode.None,
            DeviceId = deviceId,
            RequestId = requestId
        };

    public static DeviceResult Fail(
        DeviceErrorCode errorCode,
        string message,
        string? deviceId = null,
        string? requestId = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            DeviceId = deviceId,
            RequestId = requestId
        };
}

public sealed class DeviceResult<T> : DeviceResult
{
    public T? Data { get; init; }

    public static DeviceResult<T> Ok(T data, string? deviceId = null, string? requestId = null) =>
        new()
        {
            Success = true,
            ErrorCode = DeviceErrorCode.None,
            Data = data,
            DeviceId = deviceId,
            RequestId = requestId
        };

    public new static DeviceResult<T> Fail(
        DeviceErrorCode errorCode,
        string message,
        string? deviceId = null,
        string? requestId = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            DeviceId = deviceId,
            RequestId = requestId
        };
}