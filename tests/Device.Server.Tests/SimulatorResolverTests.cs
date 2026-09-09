using Device.Contracts.Capabilities;
using Device.Server.Registry;
using Device.Simulators;

namespace Device.Server.Tests;

public class SimulatorResolverTests
{
    [Fact]
    public void Resolver_CreatesPowerMeterLaserAndSwitch()
    {
        var registry = new DeviceRegistry();
        var definitions = new[]
        {
            new DeviceDefinition("PM-01", "OpticalPowerMeter", DeviceProviderKind.Simulator, SimulatorDriverResolver.PowerMeterDriver),
            new DeviceDefinition("TLS-01", "LaserSource", DeviceProviderKind.Simulator, SimulatorDriverResolver.LaserDriver),
            new DeviceDefinition("OSW-01", "OpticalSwitch", DeviceProviderKind.Simulator, SimulatorDriverResolver.SwitchDriver)
        };

        SimulatorDriverResolver.RegisterFromDefinitions(registry, definitions);

        Assert.True(registry.TryGet("PM-01", out var pm));
        Assert.IsAssignableFrom<IOpticalPowerMeter>(pm);
        Assert.True(registry.TryGet("TLS-01", out var laser));
        Assert.IsAssignableFrom<ILaserSource>(laser);
        Assert.True(registry.TryGet("OSW-01", out var sw));
        Assert.IsAssignableFrom<IOpticalSwitch>(sw);
    }
}