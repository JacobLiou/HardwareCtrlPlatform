using Station.Workflow;

namespace Station.Workflow.Tests;

/// <summary>Test double: long delay step for cancel / timeout scenarios.</summary>
internal sealed class DelayWorkflow : StationWorkflowBase
{
    private readonly TimeSpan _delay;
    private readonly WorkflowStepOptions? _stepOptions;
    private readonly Func<int, Exception?>? _failUntil;

    public DelayWorkflow(
        TimeSpan delay,
        WorkflowRunOptions? runOptions = null,
        WorkflowStepOptions? stepOptions = null,
        Func<int, Exception?>? failUntil = null)
        : base(runOptions)
    {
        _delay = delay;
        _stepOptions = stepOptions;
        _failUntil = failUntil;
    }

    public int AttemptCount { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Steps.RunAsync(
            "delay",
            async ct =>
            {
                AttemptCount++;
                var fail = _failUntil?.Invoke(AttemptCount);
                if (fail is not null)
                {
                    throw fail;
                }

                await Task.Delay(_delay, ct).ConfigureAwait(false);
            },
            _stepOptions,
            cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class ThrowingWorkflow : StationWorkflowBase
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("boom");
}

public sealed class EmptyStationWorkflowTests
{
    [Fact]
    public async Task Start_then_completes_Idle()
    {
        var workflow = new EmptyStationWorkflow();
        Assert.Equal(WorkstationState.Idle, workflow.State);

        await workflow.StartAsync();
        Assert.Equal(WorkstationState.Idle, workflow.State);
    }

    [Fact]
    public async Task Start_from_Running_is_rejected()
    {
        var workflow = new DelayWorkflow(TimeSpan.FromSeconds(5));
        var start = workflow.StartAsync();
        await Task.Delay(30);

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.StartAsync());
        await workflow.AbortAsync();
        await start;
    }
}

public sealed class StationWorkflowStabilityTests
{
    [Fact]
    public async Task Abort_mid_step_returns_Idle()
    {
        var workflow = new DelayWorkflow(TimeSpan.FromSeconds(5));
        var start = workflow.StartAsync();
        await Task.Delay(50);
        Assert.Equal(WorkstationState.Running, workflow.State);

        await workflow.AbortAsync();
        await start;

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Null(workflow.FaultInfo);
    }

    [Fact]
    public async Task Step_timeout_enters_Fault()
    {
        var workflow = new DelayWorkflow(
            TimeSpan.FromSeconds(5),
            stepOptions: new WorkflowStepOptions { Timeout = TimeSpan.FromMilliseconds(40), MaxAttempts = 1 });

        await workflow.StartAsync();

        Assert.Equal(WorkstationState.Fault, workflow.State);
        Assert.NotNull(workflow.FaultInfo);
        Assert.Contains("timed out", workflow.FaultInfo!.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Step_exception_enters_Fault()
    {
        var workflow = new ThrowingWorkflow();
        await workflow.StartAsync();

        Assert.Equal(WorkstationState.Fault, workflow.State);
        Assert.Equal("boom", workflow.FaultInfo?.Reason);
    }

    [Fact]
    public async Task Reset_from_Fault_to_Idle()
    {
        var workflow = new ThrowingWorkflow();
        await workflow.StartAsync();
        Assert.Equal(WorkstationState.Fault, workflow.State);

        await workflow.ResetAsync();
        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Null(workflow.FaultInfo);
    }

    [Fact]
    public async Task Reset_from_Idle_is_rejected()
    {
        var workflow = new EmptyStationWorkflow();
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ResetAsync());
    }

    [Fact]
    public async Task Bounded_retry_eventually_succeeds()
    {
        var workflow = new DelayWorkflow(
            TimeSpan.FromMilliseconds(1),
            stepOptions: new WorkflowStepOptions
            {
                Timeout = TimeSpan.FromSeconds(2),
                MaxAttempts = 3,
                RetryDelay = TimeSpan.FromMilliseconds(5)
            },
            failUntil: attempt => attempt < 3 ? new InvalidOperationException("transient") : null);

        await workflow.StartAsync();

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Equal(3, workflow.AttemptCount);
    }

    [Fact]
    public async Task Bounded_retry_exhausted_enters_Fault()
    {
        var workflow = new DelayWorkflow(
            TimeSpan.FromMilliseconds(1),
            stepOptions: new WorkflowStepOptions
            {
                MaxAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(5)
            },
            failUntil: _ => new InvalidOperationException("always"));

        await workflow.StartAsync();

        Assert.Equal(WorkstationState.Fault, workflow.State);
        Assert.Equal(2, workflow.AttemptCount);
    }
}

public sealed class StateTransitionGuardTests
{
    [Theory]
    [InlineData(WorkstationState.Idle, WorkstationState.Running, true)]
    [InlineData(WorkstationState.Running, WorkstationState.Idle, true)]
    [InlineData(WorkstationState.Running, WorkstationState.Fault, true)]
    [InlineData(WorkstationState.Fault, WorkstationState.Idle, true)]
    [InlineData(WorkstationState.Idle, WorkstationState.Fault, false)]
    public void CanTransition_matches_placeholder_table(WorkstationState from, WorkstationState to, bool expected)
    {
        Assert.Equal(expected, StateTransitionGuard.CanTransition(from, to));
    }
}
