namespace Device.Contracts.Common;

public sealed record DeviceCommandAuditEntry(
    DateTimeOffset TimestampUtc,
    string DeviceId,
    string ResourceId,
    string Capability,
    string Operation,
    bool Success,
    DeviceErrorCode ErrorCode,
    string? Message,
    double DurationMs,
    string? RequestId);

public interface IDeviceCommandAuditor
{
    void Record(DeviceCommandAuditEntry entry);

    IReadOnlyList<DeviceCommandAuditEntry> GetRecent(int maxCount = 200);
}
