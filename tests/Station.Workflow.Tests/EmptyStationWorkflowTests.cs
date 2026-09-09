using Station.Workflow;

namespace Station.Workflow.Tests;

public sealed class EmptyStationWorkflowTests
{
    [Fact]
    public async Task Start_then_Abort_returns_to_Idle()
    {
        var workflow = new EmptyStationWorkflow();
        Assert.Equal(WorkstationState.Idle, workflow.State);

        await workflow.StartAsync();
        Assert.Equal(WorkstationState.Running, workflow.State);

        await workflow.AbortAsync();
        Assert.Equal(WorkstationState.Idle, workflow.State);
    }

    [Fact]
    public async Task Start_from_Running_is_rejected()
    {
        var workflow = new EmptyStationWorkflow();
        await workflow.StartAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.StartAsync());
    }
}

public sealed class StateTransitionGuardTests
{
    [Theory]
    [InlineData(WorkstationState.Idle, WorkstationState.Running, true)]
    [InlineData(WorkstationState.Running, WorkstationState.Idle, true)]
    [InlineData(WorkstationState.Idle, WorkstationState.Fault, false)]
    public void CanTransition_matches_placeholder_table(WorkstationState from, WorkstationState to, bool expected)
    {
        Assert.Equal(expected, StateTransitionGuard.CanTransition(from, to));
    }
}
