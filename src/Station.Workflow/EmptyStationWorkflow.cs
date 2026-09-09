namespace Station.Workflow;

/// <summary>No-op workflow used by the Station.App template until a real station flow exists.</summary>
public sealed class EmptyStationWorkflow : IStationWorkflow
{
    private readonly object _gate = new();

    public WorkstationState State { get; private set; } = WorkstationState.Idle;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            // Start is only valid from Idle (same-state is not a start).
            if (State != WorkstationState.Idle
                || !StateTransitionGuard.CanTransition(State, WorkstationState.Running))
            {
                throw new InvalidOperationException($"Cannot start from state {State}.");
            }

            State = WorkstationState.Running;
        }

        return Task.CompletedTask;
    }

    public Task AbortAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (State == WorkstationState.Idle)
            {
                return Task.CompletedTask;
            }

            if (!StateTransitionGuard.CanTransition(State, WorkstationState.Idle))
            {
                throw new InvalidOperationException($"Cannot abort from state {State}.");
            }

            State = WorkstationState.Idle;
        }

        return Task.CompletedTask;
    }
}
