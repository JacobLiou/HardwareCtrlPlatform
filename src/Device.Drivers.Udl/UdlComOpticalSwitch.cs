using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Drivers.Udl;

public sealed class UdlComOpticalSwitch : IOpticalSwitch, IDeviceConnection
{
    public const string DriverName = "UdlComOpticalSwitch";

    private readonly UdlEngineSession _session;
    private readonly int _deviceIndex;

    public UdlComOpticalSwitch(DeviceIdentity identity, UdlEngineSession session, string? resourceId = null)
    {
        Identity = identity;
        _session = session;
        _deviceIndex = UdlChannel.ParseResourceId(resourceId);
        State = DeviceState.Online;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; private set; }

    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken) =>
        UdlDeviceConnection.ConnectAsync(Identity, _session, s => State = s, cancellationToken);

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken) =>
        UdlDeviceConnection.DisconnectAsync(Identity, s => State = s, cancellationToken);

    public Task<DeviceResult> ReconnectAsync(CancellationToken cancellationToken) =>
        UdlDeviceConnection.ReconnectAsync(DisconnectAsync, ConnectAsync, cancellationToken);

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var health = new DeviceHealth(
            State,
            IsHealthy: _session.IsOpen,
            Message: _session.IsOpen ? "UDL-COM OSW" : "UDL engine closed",
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
    }

    public Task<DeviceResult> SwitchToAsync(int inputPort, int outputPort, CancellationToken cancellationToken)
    {
        if (State == DeviceState.Offline)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.Offline,
                "UDL optical switch is offline. Call ConnectAsync first.",
                Identity.DeviceId));
        }

        if (inputPort < 0 || outputPort < 0)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.InvalidArgument,
                "Ports must be >= 0.",
                Identity.DeviceId));
        }

        try
        {
            _session.Osw.SetSwitchPosition(_deviceIndex, inputPort, outputPort);
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
