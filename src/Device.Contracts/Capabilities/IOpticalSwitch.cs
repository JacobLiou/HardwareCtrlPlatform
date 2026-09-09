using Device.Contracts.Common;

namespace Device.Contracts.Capabilities;

public interface IOpticalSwitch : IDevice
{
    Task<DeviceResult> SwitchToAsync(
        int inputPort,
        int outputPort,
        CancellationToken cancellationToken);
}