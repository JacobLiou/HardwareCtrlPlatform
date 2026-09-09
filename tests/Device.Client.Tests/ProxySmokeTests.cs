using Device.Client.Proxies;
using Device.Contracts.Common;
using Device.Server.Registry;
using Device.Simulators;

namespace Device.Client.Tests;

public class ProxySmokeTests
{
    [Fact]
    public async Task PowerMeterProxy_ReadsPower_ThroughInMemoryClient()
    {
        var (_, client) = SimulatorPlatformFactory.Create(
            new DeviceDefinition(
                "PM-01",
                "OpticalPowerMeter",
                DeviceProviderKind.Simulator,
                SimulatorDriverResolver.PowerMeterDriver,
                DisplayName: "Sim PM"));

        var proxy = new UdlPowerMeterProxy(client, new DeviceIdentity("PM-01", "OpticalPowerMeter", "Sim PM"));

        var setWl = await proxy.SetWavelengthAsync(1550, CancellationToken.None);
        Assert.True(setWl.Success);

        var power = await proxy.ReadPowerAsync(0, CancellationToken.None);
        Assert.True(power.Success);
        Assert.NotNull(power.Data);
        Assert.Equal("dBm", power.Data!.Unit);
        Assert.Equal(1550, power.Data.WavelengthNm);
    }

    [Fact]
    public async Task LaserAndSwitchProxies_Work()
    {
        var (_, client) = SimulatorPlatformFactory.Create(
            new DeviceDefinition("TLS-01", "LaserSource", DeviceProviderKind.Simulator, SimulatorDriverResolver.LaserDriver),
            new DeviceDefinition("OSW-01", "OpticalSwitch", DeviceProviderKind.Simulator, SimulatorDriverResolver.SwitchDriver));

        var laser = new UdlLaserSourceProxy(client, new DeviceIdentity("TLS-01", "LaserSource"));
        var opticalSwitch = new UdlOpticalSwitchProxy(client, new DeviceIdentity("OSW-01", "OpticalSwitch"));

        Assert.True((await laser.SetWavelengthAsync(1310, CancellationToken.None)).Success);
        Assert.True((await laser.SetOutputAsync(true, CancellationToken.None)).Success);
        Assert.True((await opticalSwitch.SwitchToAsync(1, 2, CancellationToken.None)).Success);

        var health = await laser.GetHealthAsync(CancellationToken.None);
        Assert.True(health.Success);
        Assert.Equal(DeviceState.Online, health.Data!.State);
    }

    [Fact]
    public async Task MissingDevice_ReturnsNotFound()
    {
        var (_, client) = SimulatorPlatformFactory.Create();
        var proxy = new UdlPowerMeterProxy(client, new DeviceIdentity("MISSING", "OpticalPowerMeter"));

        var result = await proxy.ReadPowerAsync(0, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(DeviceErrorCode.NotFound, result.ErrorCode);
    }
}