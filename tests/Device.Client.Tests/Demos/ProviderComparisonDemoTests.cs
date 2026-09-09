using Device.Client.Proxies;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;
using Device.Drivers.Samples;
using Device.Server.Registry;
using Device.Simulators;

namespace Device.Client.Tests.Demos;

/// <summary>
/// Side-by-side demos: same capability API + Proxy, three different providers.
/// </summary>
public class ProviderComparisonDemoTests
{
    private static readonly DeviceIdentity PmIdentity = new("PM-DEMO", "OpticalPowerMeter", "Demo Power Meter");

    /// <summary>
    /// Path A — Simulator: no hardware, fast UI/workflow development.
    /// </summary>
    [Fact]
    public async Task Demo_A_Simulator_Provider()
    {
        var meter = CreateMeterProxy(
            DeviceProviderKind.Simulator,
            SimulatorDriverResolver.PowerMeterDriver);

        var power = await ReadAt1550Async(meter);

        Assert.True(power.Success);
        Assert.Equal(-10.0, power.Data!.Value);
        Assert.Equal("dBm", power.Data.Unit);
    }

    /// <summary>
    /// Path B — UDL stub: same Proxy; behind the scenes pretends to be UDL adapter.
    /// Real ProgID/API still TODO — this only shows the insertion point.
    /// </summary>
    [Fact]
    public async Task Demo_B_UdlStub_Provider()
    {
        var meter = CreateMeterProxy(
            DeviceProviderKind.Udl,
            UdlStubPowerMeter.DriverName);

        var health = await meter.GetHealthAsync(CancellationToken.None);
        Assert.True(health.Success);
        Assert.Contains("UDL-STUB", health.Data!.Message);

        var power = await ReadAt1550Async(meter);
        Assert.True(power.Success);
        Assert.Equal(UdlStubPowerMeter.FingerprintPowerDbm, power.Data!.Value);
    }

    /// <summary>
    /// Path C — Custom inline driver: you implement IOpticalPowerMeter yourself
    /// (protocol knowledge in-house), still consumed only via Proxy + Contracts.
    /// </summary>
    [Fact]
    public async Task Demo_C_CustomInline_Provider()
    {
        var meter = CreateMeterProxy(
            DeviceProviderKind.Native,
            CustomInlinePowerMeter.DriverName);

        var health = await meter.GetHealthAsync(CancellationToken.None);
        Assert.True(health.Success);
        Assert.Contains("CUSTOM-INLINE", health.Data!.Message);

        var power = await ReadAt1550Async(meter);
        Assert.True(power.Success);
        Assert.Equal(CustomInlinePowerMeter.FingerprintPowerDbm, power.Data!.Value);
    }

    /// <summary>
    /// Same business helper against three providers — only DeviceDefinition changes.
    /// </summary>
    [Fact]
    public async Task Demo_SameBusinessCode_ThreeProviders_DifferentFingerprints()
    {
        var results = new Dictionary<string, double>();

        foreach (var (name, provider, driver) in new[]
                 {
                     ("Simulator", DeviceProviderKind.Simulator, SimulatorDriverResolver.PowerMeterDriver),
                     ("UdlStub", DeviceProviderKind.Udl, UdlStubPowerMeter.DriverName),
                     ("Custom", DeviceProviderKind.Native, CustomInlinePowerMeter.DriverName)
                 })
        {
            var meter = CreateMeterProxy(provider, driver);
            var power = await ReadAt1550Async(meter);
            Assert.True(power.Success, name);
            results[name] = power.Data!.Value;
        }

        Assert.Equal(-10.0, results["Simulator"]);
        Assert.Equal(UdlStubPowerMeter.FingerprintPowerDbm, results["UdlStub"]);
        Assert.Equal(CustomInlinePowerMeter.FingerprintPowerDbm, results["Custom"]);
        Assert.Equal(3, results.Values.Distinct().Count());
    }

    private static IOpticalPowerMeter CreateMeterProxy(DeviceProviderKind provider, string driverName)
    {
        var definition = new DeviceDefinition(
            PmIdentity.DeviceId,
            PmIdentity.DeviceType,
            provider,
            driverName,
            PmIdentity.DisplayName);

        var (_, client) = SamplePlatformFactory.Create(definition);
        return new UdlPowerMeterProxy(client, PmIdentity);
    }

    private static async Task<DeviceResult<OpticalPower>> ReadAt1550Async(IOpticalPowerMeter meter)
    {
        var set = await meter.SetWavelengthAsync(1550, CancellationToken.None);
        Assert.True(set.Success);
        return await meter.ReadPowerAsync(0, CancellationToken.None);
    }
}