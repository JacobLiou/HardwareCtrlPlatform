using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Drivers.Samples;

/// <summary>
/// Demo "self-implemented" driver: mimics a tiny inline protocol (not SerialPort).
/// </summary>
public sealed class CustomInlinePowerMeter : IOpticalPowerMeter, IDeviceConnection
{
    public const string DriverName = "CustomInlinePowerMeter";
    public const double FingerprintPowerDbm = -11.0;

    private readonly object _gate = new();
    private double _wavelengthNm = 1550;

    public CustomInlinePowerMeter(DeviceIdentity identity)
    {
        Identity = identity;
        State = DeviceState.Offline;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; private set; }

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DeviceHealth(
            State,
            IsHealthy: State == DeviceState.Online,
            Message: State == DeviceState.Online ? "CUSTOM-INLINE connected" : "CUSTOM-INLINE offline",
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
    }

    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            State = DeviceState.Online;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            State = DeviceState.Offline;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }

    public async Task<DeviceResult> ReconnectAsync(CancellationToken cancellationToken)
    {
        var d = await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        if (!d.Success)
        {
            return d;
        }

        return await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<DeviceResult<OpticalPower>> ReadPowerAsync(int channel, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (State == DeviceState.Offline)
            {
                return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                    DeviceErrorCode.Offline,
                    "Custom driver is not connected. Call ConnectAsync first.",
                    Identity.DeviceId));
            }

            if (channel < 0)
            {
                return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                    DeviceErrorCode.InvalidArgument,
                    "Channel must be >= 0.",
                    Identity.DeviceId));
            }

            var value = FingerprintPowerDbm + (_wavelengthNm - 1550) * 0.001;
            var power = new OpticalPower(value, "dBm", _wavelengthNm);
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
            if (State == DeviceState.Offline)
            {
                return Task.FromResult(DeviceResult.Fail(
                    DeviceErrorCode.Offline,
                    "Custom driver is not connected.",
                    Identity.DeviceId));
            }

            _wavelengthNm = wavelengthNm;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }
}
