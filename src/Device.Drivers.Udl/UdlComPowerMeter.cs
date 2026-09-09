using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Drivers.Udl;

public sealed class UdlComPowerMeter : IOpticalPowerMeter
{
    public const string DriverName = "UdlComPowerMeter";

    private readonly UdlEngineSession _session;
    private readonly int _wavelengthChannel;

    public UdlComPowerMeter(DeviceIdentity identity, UdlEngineSession session, string? resourceId = null)
    {
        Identity = identity;
        _session = session;
        _wavelengthChannel = UdlChannel.ParseResourceId(resourceId);
        State = DeviceState.Online;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; private set; }

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DeviceHealth(
            State,
            IsHealthy: _session.IsOpen,
            Message: _session.IsOpen ? "UDL-COM OPM" : "UDL engine closed",
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
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

        try
        {
            _session.Opm.GetPower(channel, out var power);
            if (!_session.TryGetLastError(out var err))
            {
                return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                    DeviceErrorCode.DriverError,
                    err,
                    Identity.DeviceId));
            }

            _session.Opm.GetWavelength(channel, out var wavelength);
            _ = _session.TryGetLastError(out _);

            var data = new OpticalPower(power, "dBm", wavelength > 0 ? wavelength : null);
            return Task.FromResult(DeviceResult<OpticalPower>.Ok(data, Identity.DeviceId));
        }
        catch (Exception ex)
        {
            return Task.FromResult(DeviceResult<OpticalPower>.Fail(
                DeviceErrorCode.CommunicationError,
                ex.Message,
                Identity.DeviceId));
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

        try
        {
            _session.Opm.SetWavelength(_wavelengthChannel, wavelengthNm);
            if (!_session.TryGetLastError(out var err))
            {
                return Task.FromResult(DeviceResult.Fail(
                    DeviceErrorCode.DriverError,
                    err,
                    Identity.DeviceId));
            }

            return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
        }
        catch (Exception ex)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.CommunicationError,
                ex.Message,
                Identity.DeviceId));
        }
    }
}
