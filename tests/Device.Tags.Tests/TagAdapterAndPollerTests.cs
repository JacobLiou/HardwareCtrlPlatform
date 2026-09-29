using Device.Client;
using Device.Contracts.Tags;
using Device.Hosting;
using Device.Server.Registry;
using Device.Simulators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Device.Tags.Tests;

public sealed class TagAdapterAndPollerTests
{
    [Fact]
    public async Task Poll_and_Write_via_adapters()
    {
        var (_, client) = SimulatorPlatformFactory.Create(
            new DeviceDefinition(
                "PM-01",
                "OpticalPowerMeter",
                DeviceProviderKind.Simulator,
                SimulatorDriverResolver.PowerMeterDriver),
            new DeviceDefinition(
                "TLS-01",
                "LaserSource",
                DeviceProviderKind.Simulator,
                SimulatorDriverResolver.LaserDriver));

        var devices = new[]
        {
            new DeviceDefinition("PM-01", "OpticalPowerMeter", DeviceProviderKind.Simulator, SimulatorDriverResolver.PowerMeterDriver),
            new DeviceDefinition("TLS-01", "LaserSource", DeviceProviderKind.Simulator, SimulatorDriverResolver.LaserDriver)
        };

        var store = new InMemoryTagStore();
        var powerTag = new TagDefinition
        {
            TagId = "PM-01.Power",
            DeviceId = "PM-01",
            Access = TagAccess.Read,
            Capability = "IOpticalPowerMeter",
            Operation = "ReadPower",
            Args = new Dictionary<string, object?> { ["channel"] = 0 },
            PeriodMs = 100,
            StaleAfterMs = 200,
            Unit = "dBm",
            DeviceType = "OpticalPowerMeter"
        };

        await CapabilityTagAdapter.PollAsync(powerTag, client, devices, store, CancellationToken.None);
        Assert.True(store.TryGet("PM-01.Power", out var snap));
        Assert.Equal(TagQuality.Good, snap!.Quality);
        Assert.Equal(-10.0, Convert.ToDouble(snap.Value));

        var outputTag = new TagDefinition
        {
            TagId = "TLS-01.Output",
            DeviceId = "TLS-01",
            Access = TagAccess.Write,
            Capability = "ILaserSource",
            Operation = "SetOutput",
            PeriodMs = 500,
            StaleAfterMs = 1000,
            DeviceType = "LaserSource"
        };

        var writer = new TagWriter(new TagRegistry([powerTag, outputTag]), client, devices);
        var write = await writer.WriteAsync("TLS-01.Output", true, CancellationToken.None);
        Assert.True(write.Success);
    }

    [Fact]
    public void Unknown_binding_fails_at_registry()
    {
        Assert.Throws<NotSupportedException>(() => new TagRegistry(
        [
            new TagDefinition
            {
                TagId = "X",
                DeviceId = "PM-01",
                Access = TagAccess.Read,
                Capability = "IFake",
                Operation = "Nope",
                PeriodMs = 100,
                StaleAfterMs = 200
            }
        ]));
    }

    [Fact]
    public async Task Hosting_AddTagDataPlane_registers_store_and_writer()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddDevicePlatform(o =>
        {
            o.Entries.Add(new Device.Hosting.DeviceEntryOptions
            {
                DeviceId = "PM-01",
                DeviceType = "OpticalPowerMeter",
                Provider = "Simulator",
                DriverName = SimulatorDriverResolver.PowerMeterDriver
            });
        });
        services.AddTagDataPlane(o =>
        {
            o.Entries.Add(new TagEntryOptions
            {
                TagId = "PM-01.Power",
                DeviceId = "PM-01",
                Access = "Read",
                Capability = "IOpticalPowerMeter",
                Operation = "ReadPower",
                Args = new Dictionary<string, object?> { ["channel"] = 0 },
                Unit = "dBm"
            });
        });

        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<ITagStore>();
        var writer = provider.GetRequiredService<ITagWriter>();
        var registry = provider.GetRequiredService<ITagRegistry>();
        Assert.Single(registry.Definitions);
        Assert.NotNull(store);
        Assert.NotNull(writer);

        var def = registry.Definitions[0];
        var client = provider.GetRequiredService<IUdlServerClient>();
        var devices = provider.GetRequiredService<IReadOnlyList<DeviceDefinition>>();
        await CapabilityTagAdapter.PollAsync(def, client, devices, store, CancellationToken.None);
        Assert.True(store.TryGet("PM-01.Power", out var snap));
        Assert.Equal(TagQuality.Good, snap!.Quality);
    }
}
