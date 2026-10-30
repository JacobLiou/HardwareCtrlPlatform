namespace Station.Workflow;

public enum WorkflowRunResult
{
    Completed = 0,
    Aborted = 1,
    Faulted = 2
}

/// <summary>Immutable snapshot of one finished workflow run (step traces + outcome).</summary>
public sealed record WorkflowRunTrace(
    Guid RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    WorkflowRunResult Result,
    WorkstationFaultInfo? Fault,
    IReadOnlyList<WorkflowStepTrace> Steps)
{
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;
}
