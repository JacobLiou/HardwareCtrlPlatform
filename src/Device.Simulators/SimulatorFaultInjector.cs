namespace Device.Simulators;

public sealed class FaultInjectionOptions
{
    public bool Enabled { get; set; }

    public int FixedDelayMs { get; set; }

    /// <summary>Fail the next N operations with DriverError, then clear.</summary>
    public int FailNextCount { get; set; }

    public bool ForceOffline { get; set; }

    public bool CorruptPowerReading { get; set; }
}

/// <summary>Shared fault-injection gate for Simulator devices.</summary>
public sealed class SimulatorFaultInjector
{
    private readonly object _gate = new();
    private int _failRemaining;

    public FaultInjectionOptions Options { get; private set; } = new();

    public void Configure(FaultInjectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        lock (_gate)
        {
            Options = options;
            _failRemaining = Math.Max(0, options.FailNextCount);
        }
    }

    public async Task ApplyDelayAsync(CancellationToken cancellationToken)
    {
        int delay;
        lock (_gate)
        {
            if (!Options.Enabled || Options.FixedDelayMs <= 0)
            {
                return;
            }

            delay = Options.FixedDelayMs;
        }

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    public bool ShouldForceOffline
    {
        get
        {
            lock (_gate)
            {
                return Options.Enabled && Options.ForceOffline;
            }
        }
    }

    public bool CorruptPowerReading
    {
        get
        {
            lock (_gate)
            {
                return Options.Enabled && Options.CorruptPowerReading;
            }
        }
    }

    public bool TryConsumeFailure(string deviceId, out Device.Contracts.Common.DeviceResult fail)
    {
        lock (_gate)
        {
            if (!Options.Enabled || _failRemaining <= 0)
            {
                fail = Device.Contracts.Common.DeviceResult.Ok(deviceId);
                return false;
            }

            _failRemaining--;
            fail = Device.Contracts.Common.DeviceResult.Fail(
                Device.Contracts.Common.DeviceErrorCode.DriverError,
                "Simulator fault injection: FailNextCount.",
                deviceId);
            return true;
        }
    }

    public bool TryConsumeFailure<T>(string deviceId, out Device.Contracts.Common.DeviceResult<T> fail)
    {
        if (TryConsumeFailure(deviceId, out var plain) && !plain.Success)
        {
            fail = Device.Contracts.Common.DeviceResult<T>.Fail(
                plain.ErrorCode,
                plain.Message ?? "fault",
                deviceId);
            return true;
        }

        fail = Device.Contracts.Common.DeviceResult<T>.Ok(default!, deviceId);
        return false;
    }
}
