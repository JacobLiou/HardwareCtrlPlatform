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

    public async Task<DeviceResult<OpticalPower>> ReadPowerAsync(int channel, CancellationToken cancellationToken)
    {
        var (ok, fail) = await BeginOperationAsync<OpticalPower>(cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return fail;
        }

        if (channel < 0)
        {
            return DeviceResult<OpticalPower>.Fail(
                DeviceErrorCode.InvalidArgument,
                "Channel must be >= 0.",
                Identity.DeviceId);
        }

        lock (_gate)
        {
            var value = _powerDbm;
            if (FaultInjector?.CorruptPowerReading == true)
            {
                value = double.NaN;
            }

            var power = new OpticalPower(value, "dBm", _wavelengthNm);
            return DeviceResult<OpticalPower>.Ok(power, Identity.DeviceId);
        }
    }

    public async Task<DeviceResult> SetWavelengthAsync(double wavelengthNm, CancellationToken cancellationToken)
    {
        var early = await BeginOperationAsync(cancellationToken).ConfigureAwait(false);
        if (early is not null)
        {
            return early;
        }

        if (wavelengthNm <= 0)
        {
            return DeviceResult.Fail(
                DeviceErrorCode.InvalidArgument,
                "Wavelength must be positive (nm).",
                Identity.DeviceId);
        }

        lock (_gate)
        {
            _wavelengthNm = wavelengthNm;
        }

        return DeviceResult.Ok(Identity.DeviceId);
    }

    public void SetSimulatedPowerDbm(double powerDbm)
    {
        lock (_gate)
        {
            _powerDbm = powerDbm;
        }
    }
}
