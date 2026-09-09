using Device.Client.Proxies;
using Device.Contracts.Common;
using Device.Server.Audit;
using Device.Server.Registry;
using Device.Simulators;

namespace Device.Client.Tests;

public sealed class DeviceCommandAuditTests
{
    [Fact]
    public async Task ReadPower_produces_audit_row()
    {
        var auditor = new InMemoryDeviceCommandAuditor();
        var definition = new DeviceDefinition(
            "PM-01",
            "OpticalPowerMeter",
            DeviceProviderKind.Simulator,
            SimulatorDriverResolver.PowerMeterDriver,
            ResourceId: "shared-pm");

        var (runtime, _) = SimulatorPlatformFactory.Create(definition);
        var client = new InMemoryUdlServerClient(runtime, auditor);
        var proxy = new UdlPowerMeterProxy(client, new DeviceIdentity("PM-01", "OpticalPowerMeter"));

        var power = await proxy.ReadPowerAsync(0, CancellationToken.None);
        Assert.True(power.Success);

        var recent = auditor.GetRecent(10);
        Assert.Contains(recent, e =>
            e.DeviceId == "PM-01"
            && e.ResourceId == "shared-pm"
            && e.Capability == "IOpticalPowerMeter"
            && e.Operation == "ReadPower"
            && e.Success);
    }
}
