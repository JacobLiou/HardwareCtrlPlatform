using Device.Client.Commands;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;

namespace Device.Client.Proxies;

public sealed class UdlOpticalSwitchProxy(IUdlServerClient client, DeviceIdentity identity)
    : IOpticalSwitch, IDeviceConnection
{
    public DeviceIdentity Identity { get; } = identity;

    public DeviceState State { get; private set; } = DeviceState.Unknown;

    public async Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var command = DeviceCommand.Create(Identity.DeviceId, "IDevice", "GetHealth");
        var result = await client.SendAsync<DeviceHealth>(command, cancellationToken).ConfigureAwait(false);
        if (result.Success && result.Data is not null)
        {
            State = result.Data.State;
        }

        return result;
    }

    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken) =>
        SendConnectionAsync("Connect", cancellationToken);

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken) =>
        SendConnectionAsync("Disconnect", cancellationToken);

    public Task<DeviceResult> ReconnectAsync(CancellationToken cancellationToken) =>
        SendConnectionAsync("Reconnect", cancellationToken);

    public Task<DeviceResult> SwitchToAsync(int inputPort, int outputPort, CancellationToken cancellationToken)
    {
        var command = DeviceCommand.Create(
            Identity.DeviceId,
            "IOpticalSwitch",
            "SwitchTo",
            new Dictionary<string, object?>
            {
                ["inputPort"] = inputPort,
                ["outputPort"] = outputPort
            });
        return client.SendAsync(command, cancellationToken);
    }

    private async Task<DeviceResult> SendConnectionAsync(string operation, CancellationToken cancellationToken)
    {
        var command = DeviceCommand.Create(Identity.DeviceId, "IDeviceConnection", operation);
        var result = await client.SendAsync(command, cancellationToken).ConfigureAwait(false);
        if (result.Success)
        {
            State = operation == "Disconnect" ? DeviceState.Offline : DeviceState.Online;
        }

        return result;
    }
}
