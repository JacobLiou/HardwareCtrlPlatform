namespace Station.Workflow;

public enum WorkflowStepStatus
{
    Pending = 0,
    Running = 1,
    Passed = 2,
    Failed = 3,
    Aborted = 4
}

/// <summary>
/// Execution record of one step. Mutated by <see cref="WorkflowStepRunner"/> while the step runs;
/// read by UI / session persistence. One instance per <c>Steps.RunAsync</c> call (retries update
/// <see cref="Attempt"/> on the same instance).
/// </summary>
public sealed class WorkflowStepTrace
{
    public required string Name { get; init; }

    public WorkflowStepStatus Status { get; internal set; } = WorkflowStepStatus.Pending;

    /// <summary>1-based attempt currently in progress / last attempted.</summary>
    public int Attempt { get; internal set; }

    public int MaxAttempts { get; internal set; } = 1;

    public DateTimeOffset? StartedAtUtc { get; internal set; }

    /// <summary>Total wall time from first attempt start to completion (pass / fail / abort).</summary>
    public TimeSpan? Duration { get; internal set; }

    public string? Error { get; internal set; }
}
