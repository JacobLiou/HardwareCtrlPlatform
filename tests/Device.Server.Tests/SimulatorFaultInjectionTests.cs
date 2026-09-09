using Device.Contracts.Common;
using Device.Simulators;

namespace Device.Server.Tests;

public sealed class SimulatorFaultInjectionTests
{
    [Fact]
    public async Task FixedDelay_is_observed()
    {
        var injector = new SimulatorFaultInjector();
        injector.Configure(new FaultInjectionOptions
        {
            Enabled = true,
            FixedDelayMs = 60
        });

        var meter = new SimulatedPowerMeter(new DeviceIdentity("PM-01", "OpticalPowerMeter"))
        {
            FaultInjector = injector
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await meter.ReadPowerAsync(0, CancellationToken.None);
        sw.Stop();

        Assert.True(result.Success);
        Assert.True(sw.ElapsedMilliseconds >= 50, $"elapsed={sw.ElapsedMilliseconds}");
    }

    [Fact]
    public async Task FailNextCount_returns_DriverError_then_succeeds()
    {
        var injector = new SimulatorFaultInjector();
        injector.Configure(new FaultInjectionOptions
        {
            Enabled = true,
            FailNextCount = 1
        });

        var meter = new SimulatedPowerMeter(new DeviceIdentity("PM-01", "OpticalPowerMeter"))
        {
            FaultInjector = injector
        };

        var fail = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.False(fail.Success);
        Assert.Equal(DeviceErrorCode.DriverError, fail.ErrorCode);

        var ok = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.True(ok.Success);
    }

    [Fact]
    public async Task ForceOffline_blocks_ReadPower()
    {
        var injector = new SimulatorFaultInjector();
        injector.Configure(new FaultInjectionOptions
        {
            Enabled = true,
            ForceOffline = true
        });

        var meter = new SimulatedPowerMeter(new DeviceIdentity("PM-01", "OpticalPowerMeter"))
        {
            FaultInjector = injector
        };

        var result = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(DeviceErrorCode.Offline, result.ErrorCode);
    }

    [Fact]
    public async Task CorruptPowerReading_returns_NaN()
    {
        var injector = new SimulatorFaultInjector();
        injector.Configure(new FaultInjectionOptions
        {
            Enabled = true,
            CorruptPowerReading = true
        });

        var meter = new SimulatedPowerMeter(new DeviceIdentity("PM-01", "OpticalPowerMeter"))
        {
            FaultInjector = injector
        };

        var result = await meter.ReadPowerAsync(0, CancellationToken.None);
        Assert.True(result.Success);
        Assert.True(double.IsNaN(result.Data!.Value));
    }
}
