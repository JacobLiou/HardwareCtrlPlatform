namespace Station.Workflow;

/// <summary>Station workflow use-case surface. Product stations implement or wrap this.</summary>
public interface IStationWorkflow
{
    WorkstationState State { get; }

    WorkstationFaultInfo? FaultInfo { get; }

    event EventHandler<WorkstationState>? StateChanged;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task AbortAsync(CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}
