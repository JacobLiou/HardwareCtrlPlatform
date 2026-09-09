using Device.Contracts.Common;
using Device.Drivers.Udl;
using Device.Server.Execution;

namespace Device.Hosting;

public interface IDevicePlatformLifecycle
{
    Task ConnectAllAsync(CancellationToken cancellationToken = default);

    /// <param name="forceCloseUdlSession">
    /// When true (host shutdown), also dispose the process-wide <see cref="UdlEngineSession"/>.
    /// Per-device Disconnect never tears down the shared session.
    /// </param>
    Task DisconnectAllAsync(CancellationToken cancellationToken = default, bool forceCloseUdlSession = false);
}

public sealed class DevicePlatformLifecycle(InProcessDeviceRuntime runtime) : IDevicePlatformLifecycle
{
    public async Task ConnectAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var definition in runtime.Registry.ListDefinitions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!runtime.Registry.TryGet(definition.DeviceId, out var device) || device is not IDeviceConnection connection)
            {
                continue;
            }

            var result = await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                throw new InvalidOperationException(
                    $"Connect failed for '{definition.DeviceId}': {result.ErrorCode} {result.Message}");
            }
        }
    }

    public async Task DisconnectAllAsync(CancellationToken cancellationToken = default, bool forceCloseUdlSession = false)
    {
        foreach (var definition in runtime.Registry.ListDefinitions())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!runtime.Registry.TryGet(definition.DeviceId, out var device) || device is not IDeviceConnection connection)
            {
                continue;
            }

            _ = await connection.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        if (forceCloseUdlSession)
        {
            UdlEngineSession.ForceCloseShared();
        }
    }
}
