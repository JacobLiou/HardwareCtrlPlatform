using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Station.Workflow;

namespace Station.App.ViewModels;

/// <summary>UI projection of one <see cref="WorkflowStepTrace"/> (workflow stays MVVM-free).</summary>
public partial class StepTraceItemViewModel : ObservableObject
{
    private static readonly Brush PendingBrush = Freeze(Brushes.Gray);
    private static readonly Brush RunningBrush = Freeze(Brushes.DodgerBlue);
    private static readonly Brush PassedBrush = Freeze(Brushes.Green);
    private static readonly Brush FailedBrush = Freeze(Brushes.DarkRed);
    private static readonly Brush AbortedBrush = Freeze(Brushes.DarkOrange);

    private readonly WorkflowStepTrace _trace;

    public StepTraceItemViewModel(WorkflowStepTrace trace)
    {
        _trace = trace;
        Name = trace.Name;
    }

    public string Name { get; }

    [ObservableProperty]
    private string _statusText = WorkflowStepStatus.Pending.ToString();

    [ObservableProperty]
    private string _durationText = "-";

    [ObservableProperty]
    private string _attemptsText = "";

    [ObservableProperty]
    private Brush _statusBrush = PendingBrush;

    public bool IsRunning => _trace.Status == WorkflowStepStatus.Running;

    /// <summary>Re-project the (mutable) trace after a status change event.</summary>
    public void UpdateFromTrace()
    {
        StatusText = _trace.Status.ToString();
        StatusBrush = _trace.Status switch
        {
            WorkflowStepStatus.Running => RunningBrush,
            WorkflowStepStatus.Passed => PassedBrush,
            WorkflowStepStatus.Failed => FailedBrush,
            WorkflowStepStatus.Aborted => AbortedBrush,
            _ => PendingBrush
        };
        AttemptsText = _trace.MaxAttempts > 1 ? $"{_trace.Attempt}/{_trace.MaxAttempts}" : "";
        RefreshElapsed();
        if (!string.IsNullOrEmpty(_trace.Error) && _trace.Status != WorkflowStepStatus.Running)
        {
            StatusText = $"{StatusText} — {_trace.Error}";
        }
    }

    /// <summary>Called by the run-elapsed tick so the active step shows live elapsed time.</summary>
    public void RefreshElapsed()
    {
        DurationText = _trace.Status == WorkflowStepStatus.Running && _trace.StartedAtUtc is { } started
            ? $"{(DateTimeOffset.UtcNow - started).TotalSeconds:0.0}s…"
            : _trace.Duration is { } duration
                ? $"{duration.TotalSeconds:0.0}s"
                : "-";
    }

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
