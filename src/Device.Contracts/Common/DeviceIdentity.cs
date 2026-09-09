namespace Device.Contracts.Common;

public sealed record DeviceIdentity(
    string DeviceId,
    string DeviceType,
    string? DisplayName = null);