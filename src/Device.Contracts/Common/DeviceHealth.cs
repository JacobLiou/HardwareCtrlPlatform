namespace Device.Contracts.Common;

public sealed record DeviceHealth(
    DeviceState State,
    bool IsHealthy,
    string? Message = null,
    DateTimeOffset? CheckedAt = null);