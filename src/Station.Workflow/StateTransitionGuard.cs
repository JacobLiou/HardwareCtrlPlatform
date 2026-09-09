namespace Station.Workflow;

/// <summary>Validates placeholder transitions for the empty workflow shell.</summary>
public static class StateTransitionGuard
{
    public static bool CanTransition(WorkstationState from, WorkstationState to) =>
        from == to
        || (from, to) switch
        {
            (WorkstationState.Idle, WorkstationState.Running) => true,
            (WorkstationState.Running, WorkstationState.Idle) => true,
            (WorkstationState.Running, WorkstationState.Fault) => true,
            (WorkstationState.Fault, WorkstationState.Idle) => true,
            _ => false
        };
}
