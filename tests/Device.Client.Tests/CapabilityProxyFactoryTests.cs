using Device.Client;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;
using Device.Server.Registry;
using Device.Simulators;

namespace Device.Client.Tests;

public sealed class CapabilityProxyFactoryTests
{
    [Theory]
    [InlineData("OpticalPowerMeter", typeof(IOpticalPowerMeter))]
    [InlineData("LaserSource", typeof(ILaserSource))]
    [InlineData("OpticalSwitch", typeof(IOpticalSwitch))]
    public void TryCreate_maps_known_types(string deviceType, Type expectedInterface)
    {
        var (_, client) = SimulatorPlatformFactory.Create(
            new DeviceDefinition("D-1", deviceType, DeviceProviderKind.Simulator, DriverFor(deviceType)));

        var identity = new DeviceIdentity("D-1", deviceType);
        Assert.True(CapabilityProxyFactory.TryCreate(client, identity, out var device, out var panel));
        Assert.Equal(deviceType, panel);
        Assert.IsAssignableFrom(expectedInterface, device);
    }

    [Fact]
    public void TryCreate_unknown_type_returns_false()
    {
        var (_, client) = SimulatorPlatformFactory.Create();
        Assert.False(CapabilityProxyFactory.TryCreate(
            client,
            new DeviceIdentity("X", "UnknownThing"),
            out var device,
            out var panel));
        Assert.Null(device);
        Assert.Null(panel);
    }

    private static string DriverFor(string deviceType) => deviceType switch
    {
        "OpticalPowerMeter" => SimulatorDriverResolver.PowerMeterDriver,
        "LaserSource" => SimulatorDriverResolver.LaserDriver,
        "OpticalSwitch" => SimulatorDriverResolver.SwitchDriver,
        _ => throw new ArgumentOutOfRangeException(nameof(deviceType))
    };
}
