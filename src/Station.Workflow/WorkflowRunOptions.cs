namespace Station.Workflow;

/// <summary>Defaults for a workflow run (timeouts / retry). No Polly — BCL only.</summary>
public sealed class WorkflowRunOptions
{
    public static WorkflowRunOptions Default { get; } = new();

    /// <summary>Optional timeout for the entire ExecuteAsync run.</summary>
    public TimeSpan? RunTimeout { get; init; }

    public TimeSpan DefaultStepTimeout { get; init; } = TimeSpan.FromMinutes(1);

    public int DefaultMaxAttempts { get; init; } = 1;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);
}

/// <summary>Per-step overrides for <see cref="WorkflowStepRunner"/>.</summary>
public sealed class WorkflowStepOptions
{
    public TimeSpan? Timeout { get; init; }

    public int? MaxAttempts { get; init; }

    public TimeSpan? RetryDelay { get; init; }
}
