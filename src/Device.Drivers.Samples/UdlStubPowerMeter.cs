using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Drivers.Samples;

/// <summary>
/// Demo-only stand-in for a future UDL COM/UDLServer adapter.
/// Does not call real UDL; returns a distinctive fingerprint so demos can tell providers apart.
/// </summary>
public sealed class UdlStubPowerMeter : IOpticalPowerMeter, IDeviceConnection
{
    public const string DriverName = "UdlStubPowerMeter";
    public const double FingerprintPowerDbm = -9.5;

    private double _wavelengthNm = 1550;

    public UdlStubPowerMeter(DeviceIdentity identity)
    {
        Identity = identity;
        State = DeviceState.Offline;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; private set; }

    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceState.Online;
        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceState.Offline;
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

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DeviceHealth(
            State,
            IsHealthy: State == DeviceState.Online,
            Message: State == DeviceState.Online
                ? "UDL-STUB (no real ProgID/API bound)"
                : "UDL-STUB offline",
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
    }

    public Task<DeviceResult<OpticalPower>> ReadPowerAsync(int channel, CancellationToken cancellationToken)
    {
        if (State == DeviceState.Offline)
        {
            return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                DeviceErrorCode.Offline,
                "UDL stub is not connected.",
                Identity.DeviceId));
        }

        if (channel < 0)
        {
            return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                DeviceErrorCode.InvalidArgument,
                "Channel must be >= 0.",
                Identity.DeviceId));
        }

        var power = new OpticalPower(FingerprintPowerDbm, "dBm", _wavelengthNm);
        return Task.FromResult(DeviceResult<OpticalPower>.Ok(power, Identity.DeviceId));
    }

    public Task<DeviceResult> SetWavelengthAsync(double wavelengthNm, CancellationToken cancellationToken)
    {
        if (State == DeviceState.Offline)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.Offline,
                "UDL stub is not connected.",
                Identity.DeviceId));
        }

        if (wavelengthNm <= 0)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.InvalidArgument,
                "Wavelength must be positive (nm).",
                Identity.DeviceId));
        }

        _wavelengthNm = wavelengthNm;
        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }
}
