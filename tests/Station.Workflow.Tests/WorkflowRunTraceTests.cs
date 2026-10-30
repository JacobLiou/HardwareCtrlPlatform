using Station.Workflow;

namespace Station.Workflow.Tests;

/// <summary>Test double: multi-step workflow exercising trace / event behavior.</summary>
internal sealed class MultiStepWorkflow : StationWorkflowBase
{
    private readonly Func<int, Exception?>? _failUntil;

    public MultiStepWorkflow(
        WorkflowRunOptions? runOptions = null,
        IEnumerable<IWorkflowInterlock>? interlocks = null,
        Func<int, Exception?>? failUntil = null)
        : base(runOptions, interlocks)
    {
        _failUntil = failUntil;
    }

    public int AttemptCount { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Steps.RunAsync(
            "first",
            ct => Task.Delay(10, ct),
            new WorkflowStepOptions { Timeout = TimeSpan.FromSeconds(2) },
            cancellationToken).ConfigureAwait(false);

        await Steps.RunAsync(
            "second",
            ct =>
            {
                AttemptCount++;
                var fail = _failUntil?.Invoke(AttemptCount);
                if (fail is not null)
                {
                    throw fail;
                }

                return Task.Delay(10, ct);
            },
            new WorkflowStepOptions { Timeout = TimeSpan.FromSeconds(2), MaxAttempts = 3, RetryDelay = TimeSpan.FromMilliseconds(5) },
            cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class RejectingInterlock(bool satisfied, string reason = "not ready") : IWorkflowInterlock
{
    public string Name => "TestInterlock";

    public bool IsSatisfied(out string? why)
    {
        why = satisfied ? null : reason;
        return satisfied;
    }
}

public sealed class WorkflowRunTraceTests
{
    [Fact]
    public async Task Completed_run_emits_RunCompleted_with_step_traces()
    {
        var workflow = new MultiStepWorkflow();
        WorkflowRunTrace? completed = null;

        workflow.RunCompleted += (_, trace) => completed = trace;
        await workflow.StartAsync();

        Assert.NotNull(completed);
        Assert.Equal(WorkflowRunResult.Completed, completed!.Result);
        Assert.Null(completed.Fault);
        Assert.Equal(2, completed.Steps.Count);
        Assert.All(completed.Steps, s => Assert.Equal(WorkflowStepStatus.Passed, s.Status));
        Assert.Equal("first", completed.Steps[0].Name);
        Assert.Equal("second", completed.Steps[1].Name);

        Assert.Equal(workflow.RunId, completed.RunId);
        Assert.Equal(2, workflow.StepTraces.Count);
        Assert.Equal("second", workflow.CurrentStep!.Name);
        Assert.True(completed.Duration >= TimeSpan.Zero);
        Assert.All(completed.Steps, s => Assert.True(s.Duration >= TimeSpan.Zero));
    }

    [Fact]
    public async Task Retry_updates_attempts_on_same_trace()
    {
        var workflow = new MultiStepWorkflow(failUntil: attempt => attempt < 2 ? new InvalidOperationException("transient") : null);
        var events = new List<(string Name, WorkflowStepStatus Status)>();

        // Trace objects are mutable and reused across attempts, so capture status at event time.
        workflow.StepTraceChanged += (_, trace) => events.Add((trace.Name, trace.Status));
        await workflow.StartAsync();

        var second = workflow.StepTraces.Single(s => s.Name == "second");
        Assert.Equal(WorkflowStepStatus.Passed, second.Status);
        Assert.Equal(2, second.Attempt);
        Assert.Equal(3, second.MaxAttempts);
        Assert.Equal("transient", second.Error);

        var secondEvents = events.Where(e => e.Name == "second").ToList();
        Assert.Equal(2, secondEvents.Count(e => e.Status == WorkflowStepStatus.Running));
        Assert.Equal(1, secondEvents.Count(e => e.Status == WorkflowStepStatus.Passed));
    }

    [Fact]
    public async Task Failed_step_marks_trace_Failed_and_run_Faulted()
    {
        var workflow = new MultiStepWorkflow(failUntil: _ => new InvalidOperationException("always fails"));
        WorkflowRunTrace? completed = null;

        workflow.RunCompleted += (_, trace) => completed = trace;
        await workflow.StartAsync();

        Assert.Equal(WorkstationState.Fault, workflow.State);
        Assert.NotNull(completed);
        Assert.Equal(WorkflowRunResult.Faulted, completed!.Result);
        Assert.NotNull(completed.Fault);

        var second = completed.Steps.Single(s => s.Name == "second");
        Assert.Equal(WorkflowStepStatus.Failed, second.Status);
        Assert.NotNull(second.Duration);
        Assert.Contains("always fails", second.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Abort_mid_run_emits_Aborted_with_first_step_passed()
    {
        var workflow = new DelayWorkflow(TimeSpan.FromSeconds(5));
        WorkflowRunTrace? completed = null;
        workflow.RunCompleted += (_, trace) => completed = trace;

        var start = workflow.StartAsync();
        await Task.Delay(50);
        await workflow.AbortAsync();
        await start;

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.NotNull(completed);
        Assert.Equal(WorkflowRunResult.Aborted, completed!.Result);
        Assert.Equal("delay", completed.Steps[0].Name);
        Assert.Equal(WorkflowStepStatus.Aborted, completed.Steps[0].Status);
    }

    [Fact]
    public async Task New_run_resets_RunId_and_traces()
    {
        var workflow = new MultiStepWorkflow();

        await workflow.StartAsync();
        var firstRunId = workflow.RunId;
        var firstTraces = workflow.StepTraces;

        await workflow.StartAsync();

        Assert.NotEqual(firstRunId, workflow.RunId);
        Assert.Equal(2, workflow.StepTraces.Count);
        Assert.NotSame(firstTraces[0], workflow.StepTraces[0]);
    }
}

public sealed class WorkflowInterlockTests
{
    [Fact]
    public async Task Start_is_rejected_when_interlock_not_satisfied()
    {
        var workflow = new MultiStepWorkflow(interlocks: new[] { new RejectingInterlock(satisfied: false, reason: "door open") });

        var ex = await Assert.ThrowsAsync<WorkflowStartRejectedException>(() => workflow.StartAsync());

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Single(ex.Reasons);
        Assert.Contains("door open", ex.Reasons[0], StringComparison.Ordinal);
        Assert.Equal(Guid.Empty, workflow.RunId);
        Assert.Empty(workflow.StepTraces);
    }

    [Fact]
    public async Task Start_is_allowed_when_interlock_satisfied()
    {
        var workflow = new MultiStepWorkflow(interlocks: new[] { new RejectingInterlock(satisfied: true) });

        await workflow.StartAsync();

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.NotEqual(Guid.Empty, workflow.RunId);
    }

    [Fact]
    public async Task All_failed_interlocks_are_reported()
    {
        var workflow = new MultiStepWorkflow(interlocks: new[]
        {
            new RejectingInterlock(satisfied: false, reason: "door open"),
            new RejectingInterlock(satisfied: false, reason: "air pressure low")
        });

        var ex = await Assert.ThrowsAsync<WorkflowStartRejectedException>(() => workflow.StartAsync());

        Assert.Equal(2, ex.Reasons.Count);
    }
}
