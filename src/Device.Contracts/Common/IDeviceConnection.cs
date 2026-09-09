namespace Device.Contracts.Common;

/// <summary>
/// Optional device lifecycle (not a measurement capability).
/// </summary>
public interface IDeviceConnection : IDevice
{
    Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken);

    Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken);

    Task<DeviceResult> ReconnectAsync(CancellationToken cancellationToken);
}
