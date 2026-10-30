namespace Station.Workflow;

/// <summary>Station workflow use-case surface. Product stations implement or wrap this.</summary>
public interface IStationWorkflow
{
    WorkstationState State { get; }

    WorkstationFaultInfo? FaultInfo { get; }

    /// <summary>Id of the current (or last) run.</summary>
    Guid RunId { get; }

    /// <summary>Most recently started step of the current / last run.</summary>
    WorkflowStepTrace? CurrentStep { get; }

    /// <summary>Snapshot copy of the current / last run's step traces.</summary>
    IReadOnlyList<WorkflowStepTrace> StepTraces { get; }

    event EventHandler<WorkstationState>? StateChanged;

    event EventHandler<WorkflowStepTrace>? StepTraceChanged;

    event EventHandler<WorkflowRunTrace>? RunCompleted;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task AbortAsync(CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}
