using Device.Contracts.Common;

namespace Device.Simulators;

public abstract class SimulatedDeviceBase : IDevice
{
    protected SimulatedDeviceBase(DeviceIdentity identity)
    {
        Identity = identity;
        State = DeviceState.Online;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; protected set; }

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DeviceHealth(
            State,
            State is DeviceState.Online or DeviceState.Busy,
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
    }
}