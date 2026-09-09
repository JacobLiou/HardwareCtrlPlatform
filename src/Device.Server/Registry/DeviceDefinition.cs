namespace Device.Server.Registry;

public enum DeviceProviderKind
{
    Simulator,
    Udl,
    LegacyUdlCom,
    LegacyDeviceControl,
    Native
}

public sealed record DeviceDefinition(
    string DeviceId,
    string DeviceType,
    DeviceProviderKind Provider,
    string DriverName,
    string? DisplayName = null,
    string? ResourceId = null);