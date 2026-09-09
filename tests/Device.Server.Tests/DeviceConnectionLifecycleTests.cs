using Device.Contracts.Capabilities;
using Device.Contracts.Common;
using Device.Simulators;

namespace Device.Server.Tests;

public sealed class DeviceConnectionLifecycleTests
{
    [Fact]
    public async Task Disconnect_blocks_ReadPower_until_Connect()
    {
        var meter = new SimulatedPowerMeter(new DeviceIdentity("PM-01", "OpticalPowerMeter"));

        Assert.True((await meter.DisconnectAsync(CancellationToken.None)).Success);

        var offline = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.False(offline.Success);
        Assert.Equal(DeviceErrorCode.Offline, offline.ErrorCode);

        Assert.True((await meter.ConnectAsync(CancellationToken.None)).Success);

        var online = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.True(online.Success);
        Assert.NotNull(online.Data);
    }

    [Fact]
    public async Task Reconnect_restores_Online()
    {
        var laser = new SimulatedLaserSource(new DeviceIdentity("TLS-01", "LaserSource"));
        Assert.True((await laser.DisconnectAsync(CancellationToken.None)).Success);
        Assert.True((await laser.ReconnectAsync(CancellationToken.None)).Success);

        var health = await laser.GetHealthAsync(CancellationToken.None);
        Assert.Equal(DeviceState.Online, health.Data!.State);
        Assert.True((await laser.SetOutputAsync(true, CancellationToken.None)).Success);
    }
}
