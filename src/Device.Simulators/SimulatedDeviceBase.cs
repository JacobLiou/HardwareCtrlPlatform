using Device.Contracts.Common;

namespace Device.Simulators;

public abstract class SimulatedDeviceBase : IDeviceConnection
{
    private readonly object _connectionGate = new();

    protected SimulatedDeviceBase(DeviceIdentity identity)
    {
        Identity = identity;
        State = DeviceState.Online;
    }

    public DeviceIdentity Identity { get; }

    public DeviceState State { get; protected set; }

    /// <summary>Optional fault injection assigned by Hosting for Simulator devices.</summary>
    public SimulatorFaultInjector? FaultInjector { get; set; }

    public Task<DeviceResult<DeviceHealth>> GetHealthAsync(CancellationToken cancellationToken)
    {
        var effective = GetEffectiveState();
        var health = new DeviceHealth(
            effective,
            effective is DeviceState.Online or DeviceState.Busy,
            CheckedAt: DateTimeOffset.UtcNow);
        return Task.FromResult(DeviceResult<DeviceHealth>.Ok(health, Identity.DeviceId));
    }

    public Task<DeviceResult> ConnectAsync(CancellationToken cancellationToken)
    {
        lock (_connectionGate)
        {
            State = DeviceState.Online;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }

    public Task<DeviceResult> DisconnectAsync(CancellationToken cancellationToken)
    {
        lock (_connectionGate)
        {
            State = DeviceState.Offline;
        }

        return Task.FromResult(DeviceResult.Ok(Identity.DeviceId));
    }

    public async Task<DeviceResult> ReconnectAsync(CancellationToken cancellationToken)
    {
        var disconnect = await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        if (!disconnect.Success)
        {
            return disconnect;
        }

        return await ConnectAsync(cancellationToken).ConfigureAwait(false);
    }

    protected DeviceState GetEffectiveState()
    {
        if (FaultInjector?.ShouldForceOffline == true)
        {
            return DeviceState.Offline;
        }

        return State;
    }

    protected async Task<DeviceResult?> BeginOperationAsync(CancellationToken cancellationToken)
    {
        if (FaultInjector is not null)
        {
            await FaultInjector.ApplyDelayAsync(cancellationToken).ConfigureAwait(false);
            if (FaultInjector.TryConsumeFailure(Identity.DeviceId, out var fail) && !fail.Success)
            {
                return fail;
            }
        }

        if (GetEffectiveState() == DeviceState.Offline)
        {
            return DeviceResult.Fail(
                DeviceErrorCode.Offline,
                $"Device '{Identity.DeviceId}' is offline.",
                Identity.DeviceId);
        }

        return null;
    }

    protected async Task<(bool Ok, DeviceResult<T> Fail)> BeginOperationAsync<T>(CancellationToken cancellationToken)
    {
        if (FaultInjector is not null)
        {
            await FaultInjector.ApplyDelayAsync(cancellationToken).ConfigureAwait(false);
            if (FaultInjector.TryConsumeFailure<T>(Identity.DeviceId, out var fail) && !fail.Success)
            {
                return (false, fail);
            }
        }

        if (GetEffectiveState() == DeviceState.Offline)
        {
            return (false, DeviceResult<T>.Fail(
                DeviceErrorCode.Offline,
                $"Device '{Identity.DeviceId}' is offline.",
                Identity.DeviceId));
        }

        return (true, DeviceResult<T>.Ok(default!, Identity.DeviceId));
    }
}
