using System.IO;
using Device.Contracts.Tags;
using Device.Hosting;
using Device.Tags;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Station.App.ViewModels;
using Station.App.Workflow;
using Station.Session;
using Station.Workflow;

namespace Station.App.Tests;

public sealed class TemplateStationWorkflowTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hcp-flow-{Guid.NewGuid():N}");

    [Fact]
    public async Task Run_produces_three_passed_steps_and_pending_session()
    {
        var tags = new InMemoryTagStore();
        tags.Publish(new TagSnapshot("PM-01.Power", -3.25, "dBm", TagQuality.Good, DateTimeOffset.UtcNow));

        var workflow = new TemplateStationWorkflow(tags, new[] { new TagsReadyInterlock(tags) });
        var (recorder, store) = CreateRecorder(workflow);

        await workflow.StartAsync();
        await recorder.LastSave!;

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Equal(new[] { "SelfTest", "Sample", "Report" }, workflow.StepTraces.Select(s => s.Name));
        Assert.All(workflow.StepTraces, s => Assert.Equal(WorkflowStepStatus.Passed, s.Status));
        Assert.NotNull(workflow.LastSample);

        Assert.Single(await store.ListPendingAsync());
    }

    [Fact]
    public async Task Start_is_rejected_without_good_tags()
    {
        var tags = new InMemoryTagStore();
        var workflow = new TemplateStationWorkflow(tags, new[] { new TagsReadyInterlock(tags) });

        await Assert.ThrowsAsync<WorkflowStartRejectedException>(() => workflow.StartAsync());

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Empty(workflow.StepTraces);
    }

    private (SessionRecorder Recorder, JsonSessionStore Store) CreateRecorder(TemplateStationWorkflow workflow)
    {
        var options = Options.Create(new SessionOptions { RootDirectory = _root, StationId = "ST-T" });
        var store = new JsonSessionStore(options);
        return (new SessionRecorder(workflow, store, options), store);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

public sealed class MainViewModelTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"hcp-vm-{Guid.NewGuid():N}");

    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Station:DisplayName"] = "Test Station"
            })
            .Build();

    private MainViewModel CreateViewModel(
        TemplateStationWorkflow workflow,
        SessionRecorder recorder) =>
        new(workflow, CreateConfig(), new DeviceRegistrationSummary(3), recorder);

    private (TemplateStationWorkflow Workflow, SessionRecorder Recorder) CreateWorkflowAndRecorder(ITagStore tags)
    {
        var workflow = new TemplateStationWorkflow(tags, new[] { new TagsReadyInterlock(tags) });
        var options = Options.Create(new SessionOptions { RootDirectory = _root, StationId = "ST-VM" });
        var recorder = new SessionRecorder(workflow, new JsonSessionStore(options), options);
        return (workflow, recorder);
    }

    [Fact]
    public async Task Start_rejection_surfaces_interlock_text()
    {
        var tags = new InMemoryTagStore();
        var (workflow, recorder) = CreateWorkflowAndRecorder(tags);
        var vm = CreateViewModel(workflow, recorder);

        await vm.StartCommand.ExecuteAsync(null);

        Assert.Equal(WorkstationState.Idle, workflow.State);
        Assert.Contains("TagsReady", vm.InterlockText, StringComparison.Ordinal);
        Assert.Contains(vm.RunLog, line => line.Contains("Start rejected", StringComparison.Ordinal));
        Assert.Empty(vm.Steps);
    }

    [Fact]
    public async Task Completed_run_populates_steps_log_and_session_text()
    {
        var tags = new InMemoryTagStore();
        tags.Publish(new TagSnapshot("PM-01.Power", -3.25, "dBm", TagQuality.Good, DateTimeOffset.UtcNow));
        var (workflow, recorder) = CreateWorkflowAndRecorder(tags);
        var vm = CreateViewModel(workflow, recorder);

        await vm.StartCommand.ExecuteAsync(null);
        await recorder.LastSave!;

        Assert.Equal(3, vm.Steps.Count);
        Assert.All(vm.Steps, s => Assert.Equal(WorkflowStepStatus.Passed.ToString(), s.StatusText));
        Assert.Contains(vm.RunLog, line => line.Contains("Completed", StringComparison.Ordinal));
        Assert.Contains(vm.RunLog, line => line.Contains("Step SelfTest", StringComparison.Ordinal));
        Assert.Contains("saved to pending", vm.LastSessionText, StringComparison.Ordinal);
        Assert.True(vm.CanStart);
        Assert.False(vm.CanReset);
    }

    [Fact]
    public async Task Faulted_run_marks_failing_step_in_red_state()
    {
        var tags = new InMemoryTagStore();
        // Good tag exists so Start passes, but sampling fails: publish then flip quality to Bad.
        tags.Publish(new TagSnapshot("PM-01.Power", -3.25, "dBm", TagQuality.Good, DateTimeOffset.UtcNow));
        var (workflow, recorder) = CreateWorkflowAndRecorder(tags);
        var vm = CreateViewModel(workflow, recorder);

        // Simulate data going bad between Start and the Sample step.
        workflow.StepTraceChanged += (_, trace) =>
        {
            if (trace.Name == "SelfTest")
            {
                tags.Publish(new TagSnapshot("PM-01.Power", -3.25, "dBm", TagQuality.Bad, DateTimeOffset.UtcNow));
            }
        };

        await vm.StartCommand.ExecuteAsync(null);
        await recorder.LastSave!;

        Assert.Equal(WorkstationState.Fault, workflow.State);
        Assert.True(vm.CanReset);
        Assert.Contains(vm.Steps, s => s.StatusText.Contains("Failed", StringComparison.Ordinal));
        Assert.Contains("Faulted", vm.LastSessionText, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
