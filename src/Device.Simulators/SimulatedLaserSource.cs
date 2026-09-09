using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Simulators;

public sealed class SimulatedLaserSource : SimulatedDeviceBase, ILaserSource
{
    private readonly object _gate = new();
    private double _wavelengthNm = 1550;
    private bool _outputEnabled;

    public SimulatedLaserSource(DeviceIdentity identity)
        : base(identity)
    {
    }

    public double WavelengthNm
    {
        get
        {
            lock (_gate)
            {
                return _wavelengthNm;
            }
        }
    }

    public bool OutputEnabled
    {
        get
        {
            lock (_gate)
            {
                return _outputEnabled;
            }
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

    public Task<DeviceResult> SetOutputAsync(bool enabled, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _outputEnabled = enabled;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }
}