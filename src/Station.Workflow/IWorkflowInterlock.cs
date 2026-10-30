namespace Station.Workflow;

/// <summary>
/// Start gate: every interlock must be satisfied before a run may start.
/// Evaluated synchronously under the workflow state lock — keep it quick and non-blocking.
/// </summary>
public interface IWorkflowInterlock
{
    string Name { get; }

    /// <param name="reason">Human-readable why-not text shown when Start is rejected.</param>
    bool IsSatisfied(out string? reason);
}

/// <summary>Thrown by <c>StartAsync</c> when one or more interlocks are not satisfied.</summary>
public sealed class WorkflowStartRejectedException : InvalidOperationException
{
    public WorkflowStartRejectedException(IReadOnlyList<string> reasons)
        : base($"Start rejected by interlock: {string.Join("; ", reasons)}")
        => Reasons = reasons;

    public IReadOnlyList<string> Reasons { get; }
}
