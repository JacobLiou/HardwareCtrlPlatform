namespace Device.Client.Commands;

public sealed record DeviceCommand(
    string DeviceId,
    string Capability,
    string Operation,
    IReadOnlyDictionary<string, object?> Arguments,
    string? RequestId = null)
{
    public static DeviceCommand Create(
        string deviceId,
        string capability,
        string operation,
        IReadOnlyDictionary<string, object?>? arguments = null,
        string? requestId = null) =>
        new(
            deviceId,
            capability,
            operation,
            arguments ?? new Dictionary<string, object?>(),
            requestId ?? Guid.NewGuid().ToString("N"));
}