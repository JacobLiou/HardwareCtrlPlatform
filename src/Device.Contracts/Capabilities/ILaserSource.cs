using Device.Contracts.Common;

namespace Device.Contracts.Capabilities;

public interface ILaserSource : IDevice
{
    Task<DeviceResult> SetWavelengthAsync(
        double wavelengthNm,
        CancellationToken cancellationToken);

    Task<DeviceResult> SetOutputAsync(
        bool enabled,
        CancellationToken cancellationToken);
}