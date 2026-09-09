namespace Station.Workflow;

/// <summary>No-op workflow used by the Station.App template until a real station flow exists.</summary>
public sealed class EmptyStationWorkflow : StationWorkflowBase
{
    public EmptyStationWorkflow(WorkflowRunOptions? options = null)
        : base(options)
    {
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
