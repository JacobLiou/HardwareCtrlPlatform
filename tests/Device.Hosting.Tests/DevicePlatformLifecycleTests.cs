using Device.Client.Proxies;
using Device.Contracts.Common;
using Device.Hosting;
using Device.Server.Execution;
using Device.Server.Registry;
using Device.Simulators;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Hosting.Tests;

public sealed class DevicePlatformLifecycleTests
{
    [Fact]
    public async Task ConnectAll_DisconnectAll_round_trip()
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
                DriverName = SimulatorDriverResolver.PowerMeterDriver
            });
        });

        await using var provider = services.BuildServiceProvider();
        var lifecycle = provider.GetRequiredService<IDevicePlatformLifecycle>();
        var runtime = provider.GetRequiredService<InProcessDeviceRuntime>();

        await lifecycle.DisconnectAllAsync();
        Assert.True(runtime.Registry.TryGet("PM-01", out var device));
        Assert.Equal(DeviceState.Offline, device!.State);

        await lifecycle.ConnectAllAsync();
        Assert.Equal(DeviceState.Online, device.State);
    }

    [Fact]
    public async Task Shared_ResourceId_serializes_through_runtime()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddDevicePlatform(o =>
        {
            o.Entries.Add(new DeviceEntryOptions
            {
                DeviceId = "PM-A",
                DeviceType = "OpticalPowerMeter",
                Provider = "Simulator",
                DriverName = SimulatorDriverResolver.PowerMeterDriver,
                ResourceId = "shared-lock"
            });
            o.Entries.Add(new DeviceEntryOptions
            {
                DeviceId = "PM-B",
                DeviceType = "OpticalPowerMeter",
                Provider = "Simulator",
                DriverName = SimulatorDriverResolver.PowerMeterDriver,
                ResourceId = "shared-lock"
            });
        });

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<InProcessDeviceRuntime>();
        Assert.Equal("shared-lock", runtime.ResolveResourceId("PM-A"));
        Assert.Equal("shared-lock", runtime.ResolveResourceId("PM-B"));

        var timeline = new List<string>();
        var gate = new object();

        async Task<DeviceResult<int>> Slow(string label, CancellationToken ct)
        {
            lock (gate)
            {
                timeline.Add($"{label}:start");
            }

            await Task.Delay(80, ct);

            lock (gate)
            {
                timeline.Add($"{label}:end");
            }

            return DeviceResult<int>.Ok(1, label);
        }

        var t1 = runtime.InvokeAsync("PM-A", (_, ct) => Slow("A", ct), CancellationToken.None);
        await Task.Delay(10);
        var t2 = runtime.InvokeAsync("PM-B", (_, ct) => Slow("B", ct), CancellationToken.None);
        await Task.WhenAll(t1, t2);

        Assert.Equal(["A:start", "A:end", "B:start", "B:end"], timeline);
    }
}

public sealed class FaultInjectionHostingTests
{
    [Fact]
    public async Task Devices_FaultInjection_applies_to_simulator()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddDevicePlatform(o =>
        {
            o.FaultInjection = new FaultInjectionOptions
            {
                Enabled = true,
                FailNextCount = 1
            };
            o.Entries.Add(new DeviceEntryOptions
            {
                DeviceId = "PM-01",
                DeviceType = "OpticalPowerMeter",
                Provider = "Simulator",
                DriverName = SimulatorDriverResolver.PowerMeterDriver
            });
        });

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<Device.Client.IUdlServerClient>();
        var proxy = new UdlPowerMeterProxy(client, new DeviceIdentity("PM-01", "OpticalPowerMeter"));

        var fail = await proxy.ReadPowerAsync(0, CancellationToken.None);
        Assert.False(fail.Success);
        Assert.Equal(DeviceErrorCode.DriverError, fail.ErrorCode);

        var ok = await proxy.ReadPowerAsync(0, CancellationToken.None);
        Assert.True(ok.Success);
    }
}
