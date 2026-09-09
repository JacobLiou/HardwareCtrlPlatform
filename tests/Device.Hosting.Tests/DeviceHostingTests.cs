using Device.Contracts.Capabilities;
using Device.Drivers.Samples;
using Device.Hosting;
using Device.Server.Registry;
using Device.Simulators;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Hosting.Tests;

public sealed class CompositeDriverResolverTests
{
    [Fact]
    public void Resolve_simulator_power_meter()
    {
        var resolver = new CompositeDriverResolver();
        var device = resolver.Resolve(new DeviceDefinition(
            "PM-01",
            "OpticalPowerMeter",
            DeviceProviderKind.Simulator,
            SimulatorDriverResolver.PowerMeterDriver));

        Assert.IsAssignableFrom<IOpticalPowerMeter>(device);
    }

    [Fact]
    public void Resolve_native_custom_power_meter()
    {
        var resolver = new CompositeDriverResolver();
        var device = resolver.Resolve(new DeviceDefinition(
            "PM-N",
            "OpticalPowerMeter",
            DeviceProviderKind.Native,
            CustomInlinePowerMeter.DriverName));

        Assert.IsType<CustomInlinePowerMeter>(device);
    }

    [Fact]
    public void Resolve_unknown_native_driver_throws()
    {
        var resolver = new CompositeDriverResolver();
        Assert.Throws<NotSupportedException>(() => resolver.Resolve(new DeviceDefinition(
            "X-1",
            "OpticalPowerMeter",
            DeviceProviderKind.Native,
            "DoesNotExist")));
    }
}

public sealed class AddDevicePlatformTests
{
    [Fact]
    public void AddDevicePlatform_registers_client_and_summary()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddDevicePlatform(options =>
        {
            options.Entries.Add(new DeviceEntryOptions
            {
                DeviceId = "PM-01",
                DeviceType = "OpticalPowerMeter",
                Provider = "Simulator",
                DriverName = SimulatorDriverResolver.PowerMeterDriver
            });
        });

        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<Device.Client.IUdlServerClient>();
        var summary = provider.GetRequiredService<IDeviceRegistrationSummary>();
        var definitions = provider.GetRequiredService<IReadOnlyList<DeviceDefinition>>();

        Assert.NotNull(client);
        Assert.Equal(1, summary.Count);
        Assert.Single(definitions);
        Assert.Equal("PM-01", definitions[0].DeviceId);
    }
}
