using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Simulators;

public sealed class SimulatedPowerMeter : SimulatedDeviceBase, IOpticalPowerMeter
{
    private readonly object _gate = new();
    private double _wavelengthNm = 1550;
    private double _powerDbm = -10.0;

    public SimulatedPowerMeter(DeviceIdentity identity)
        : base(identity)
    {
    }

    public Task<DeviceResult<OpticalPower>> ReadPowerAsync(int channel, CancellationToken cancellationToken)
    {
        if (channel < 0)
        {
            return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                DeviceErrorCode.InvalidArgument,
                "Channel must be >= 0.",
                Identity.DeviceId));
        }

        lock (_gate)
        {
            var power = new OpticalPower(_powerDbm, "dBm", _wavelengthNm);
            return Task.FromResult(DeviceResult<OpticalPower>.Ok(power, Identity.DeviceId));
        }
    }

    public Task<DeviceResult> SetWavelengthAsync(double wavelengthNm, CancellationToken cancellationToken)
    {
        if (wavelengthNm <= 0)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.InvalidArgument,
                "Wavelength must be positive (nm).",
                Identity.DeviceId));
        }

        lock (_gate)
        {
            _wavelengthNm = wavelengthNm;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }

    /// <summary>Test helper to inject a power reading.</summary>
    public void SetSimulatedPowerDbm(double powerDbm)
    {
        lock (_gate)
        {
            _powerDbm = powerDbm;
        }
    }
}