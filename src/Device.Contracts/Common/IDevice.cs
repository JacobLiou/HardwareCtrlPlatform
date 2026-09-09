namespace Device.Contracts.Common;

public interface IDevice
{
    DeviceIdentity Identity { get; }

    DeviceState State { get; }

    Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken);
}