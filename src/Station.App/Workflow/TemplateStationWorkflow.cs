using Device.Contracts.Tags;
using Station.Workflow;

namespace Station.App.Workflow;

/// <summary>
/// Copyable three-step template flow: SelfTest → Sample (tag data plane) → Report.
/// Demonstrates the intended <see cref="StationWorkflowBase"/> step pattern without product specifics.
/// Replace with a real station flow by editing <see cref="ExecuteAsync"/> only.
/// </summary>
public sealed class TemplateStationWorkflow : StationWorkflowBase
{
    private readonly ITagStore _tagStore;

    public TemplateStationWorkflow(
        ITagStore tagStore,
        IEnumerable<IWorkflowInterlock>? interlocks = null,
        WorkflowRunOptions? options = null)
        : base(options, interlocks)
    {
        _tagStore = tagStore;
    }

    /// <summary>Good-quality tag snapshots captured by the last Sample step.</summary>
    public IReadOnlyList<TagSnapshot>? LastSample { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Steps.RunAsync(
            "SelfTest",
            ct => Task.Delay(100, ct),
            new WorkflowStepOptions { Timeout = TimeSpan.FromSeconds(5) },
            cancellationToken).ConfigureAwait(false);

        await Steps.RunAsync(
            "Sample",
            ct =>
            {
                var good = _tagStore.Snapshot().Where(t => t.Quality == TagQuality.Good).ToList();
                if (good.Count == 0)
                {
                    throw new InvalidOperationException("No Good-quality tags available to sample.");
                }

                LastSample = good;
                return Task.CompletedTask;
            },
            new WorkflowStepOptions { Timeout = TimeSpan.FromSeconds(5), MaxAttempts = 3, RetryDelay = TimeSpan.FromMilliseconds(200) },
            cancellationToken).ConfigureAwait(false);

        await Steps.RunAsync(
            "Report",
            ct => Task.Delay(100, ct),
            new WorkflowStepOptions { Timeout = TimeSpan.FromSeconds(5) },
            cancellationToken).ConfigureAwait(false);
    }
}
