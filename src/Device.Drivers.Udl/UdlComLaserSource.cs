using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Drivers.Udl;

public sealed class UdlComLaserSource : ILaserSource
{
    public const string DriverName = "UdlComLaserSource";

    private readonly UdlEngineSession _session;
    private readonly int _channel;

    public UdlComLaserSource(DeviceIdentity identity, UdlEngineSession session, string? resourceId = null)
    {
        Identity = identity;
        _session = session;
        _channel = UdlChannel.ParseResourceId(resourceId);
        State = DeviceState.Online;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; private set; }

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DeviceHealth(
            State,
            IsHealthy: _session.IsOpen,
            Message: _session.IsOpen ? "UDL-COM TLS" : "UDL engine closed",
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
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
            _session.Tls.SetTLSWavelength(_channel, wavelengthNm);
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

    public Task<DeviceResult> SetOutputAsync(bool enabled, CancellationToken cancellationToken)
    {
        try
        {
            _session.Tls.SetTLSOutputEnable(_channel, enabled ? 1 : 0);
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
