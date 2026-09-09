using Device.Contracts.Common;

namespace Device.Drivers.Udl;

internal static class UdlDeviceConnection
{
    public static Task<DeviceResult> ConnectAsync(
        DeviceIdentity identity,
        UdlEngineSession session,
        Action<DeviceState> setState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!session.IsOpen)
            {
                return Task.FromResult(DeviceResult.Fail(
                    DeviceErrorCode.CommunicationError,
                    "UDL engine session is not open.",
                    identity.DeviceId));
            }

            setState(DeviceState.Online);
            return Task.FromResult(DeviceResult.Ok(identity.DeviceId));
        }
        catch (Exception ex)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.CommunicationError,
                ex.Message,
                identity.DeviceId));
        }
    }

    public static Task<DeviceResult> DisconnectAsync(
        DeviceIdentity identity,
        Action<DeviceState> setState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Do not dispose process-wide UdlEngineSession here.
        setState(DeviceState.Offline);
        return Task.FromResult(DeviceResult.Ok(identity.DeviceId));
    }

    public static async Task<DeviceResult> ReconnectAsync(
        Func<CancellationToken, Task<DeviceResult>> disconnect,
        Func<CancellationToken, Task<DeviceResult>> connect,
        CancellationToken cancellationToken)
    {
        var d = await disconnect(cancellationToken).ConfigureAwait(false);
        if (!d.Success)
        {
            return d;
        }

        return await connect(cancellationToken).ConfigureAwait(false);
    }
}
