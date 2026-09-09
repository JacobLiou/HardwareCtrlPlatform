using Device.Hosting;
using Device.Server.Registry;
using Device.Simulators;
using Microsoft.Extensions.DependencyInjection;
using Station.App.ViewModels;

namespace Station.App.Tests;

public sealed class DeviceDebugViewModelTests
{
    [Fact]
    public async Task ReadPower_on_simulator_power_meter_succeeds()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddDevicePlatform(o =>
        {
            o.Entries.Add(new DeviceEntryOptions
            {
                DeviceId = "PM-01",
                DeviceType = "OpticalPowerMeter",
                Provider = "Simulator",
                DriverName = SimulatorDriverResolver.PowerMeterDriver,
                DisplayName = "Test PM"
            });
        });

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Device.Client.IUdlServerClient>();
        var definitions = provider.GetRequiredService<IReadOnlyList<DeviceDefinition>>();
        var vm = new DeviceDebugViewModel(client, definitions);

        Assert.Single(vm.Devices);
        vm.SelectedDevice = vm.Devices[0];
        Assert.True(vm.IsPowerMeter);

        await vm.ReadPowerCommand.ExecuteAsync(null);

        Assert.Contains("dBm", vm.UplinkValue, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReadPower OK", vm.LogText, StringComparison.Ordinal);
    }
}
