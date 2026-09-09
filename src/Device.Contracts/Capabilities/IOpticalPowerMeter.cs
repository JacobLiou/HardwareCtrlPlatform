using Device.Contracts.Common;

namespace Device.Contracts.Capabilities;

public interface IOpticalPowerMeter : IDevice
{
    Task<DeviceResult<OpticalPower>> ReadPowerAsync(
        int channel,
        CancellationToken cancellationToken);

    Task<DeviceResult> SetWavelengthAsync(
        double wavelengthNm,
        CancellationToken cancellationToken);
}