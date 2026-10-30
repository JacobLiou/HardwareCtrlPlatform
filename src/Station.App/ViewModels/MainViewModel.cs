using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Device.Hosting;
using Microsoft.Extensions.Configuration;
using Station.Session;
using Station.Workflow;

namespace Station.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const int MaxLogLines = 200;

    private readonly IStationWorkflow _workflow;
    private readonly SessionRecorder? _sessionRecorder;
    private readonly Dictionary<WorkflowStepTrace, StepTraceItemViewModel> _stepItems = new();
    private readonly DispatcherTimer? _elapsedTimer;
    private DateTimeOffset _runStartedAt = DateTimeOffset.MinValue;

    public MainViewModel(
        IStationWorkflow workflow,
        IConfiguration configuration,
        IDeviceRegistrationSummary devices,
        SessionRecorder? sessionRecorder = null)
    {
        _workflow = workflow;
        _sessionRecorder = sessionRecorder;

        Title = configuration["Station:DisplayName"] ?? "Station Template";
        DevicesText = $"Devices registered: {devices.Count}";

        _workflow.StateChanged += (_, state) => OnUi(() => RefreshState(state));
        _workflow.StepTraceChanged += (_, trace) => OnUi(() => HandleStepTrace(trace));
        _workflow.RunCompleted += (_, trace) => OnUi(() => HandleRunCompleted(trace));
        if (_sessionRecorder is not null)
        {
            _sessionRecorder.SessionSaved += (_, e) => OnUi(() => HandleSessionSaved(e));
        }

        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _elapsedTimer.Tick += (_, _) => RefreshElapsed();

        RefreshState(_workflow.State);
    }

    [ObservableProperty]
    private string _title = "Station Template";

    [ObservableProperty]
    private string _stateText = WorkstationState.Idle.ToString();

    [ObservableProperty]
    private string _faultText = "";

    [ObservableProperty]
    private string _interlockText = "";

    [ObservableProperty]
    private string _devicesText = "Devices registered: 0";

    [ObservableProperty]
    private string _runElapsedText = "";

    [ObservableProperty]
    private string _lastSessionText = "No session saved yet";

    public ObservableCollection<StepTraceItemViewModel> Steps { get; } = new();

    public ObservableCollection<string> RunLog { get; } = new();

    [ObservableProperty]
    private bool _canStart;

    [ObservableProperty]
    private bool _canAbort;

    [ObservableProperty]
    private bool _canReset;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        try
        {
            await _workflow.StartAsync();
        }
        catch (WorkflowStartRejectedException ex)
        {
            InterlockText = string.Join("; ", ex.Reasons);
            Log($"Start rejected: {InterlockText}");
            return;
        }
        finally
        {
            RefreshState(_workflow.State);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAbort))]
    private async Task AbortAsync()
    {
        await _workflow.AbortAsync();
        RefreshState(_workflow.State);
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private async Task ResetAsync()
    {
        await _workflow.ResetAsync();
        RefreshState(_workflow.State);
    }

    private void RefreshState(WorkstationState state)
    {
        StateText = state.ToString();
        FaultText = _workflow.FaultInfo is null
            ? ""
            : $"Fault: {_workflow.FaultInfo.Reason}";
        CanStart = state == WorkstationState.Idle;
        CanAbort = state == WorkstationState.Running;
        CanReset = state == WorkstationState.Fault;

        StartCommand.NotifyCanExecuteChanged();
        AbortCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();

        if (state == WorkstationState.Running)
        {
            InterlockText = "";
            _runStartedAt = DateTimeOffset.UtcNow;
            _stepItems.Clear();
            Steps.Clear();
            RunLog.Clear();
            Log($"Run {_workflow.RunId:N} started");
            _elapsedTimer?.Start();
        }
        else
        {
            _elapsedTimer?.Stop();
        }
    }

    private void HandleStepTrace(WorkflowStepTrace trace)
    {
        if (!_stepItems.TryGetValue(trace, out var item))
        {
            item = new StepTraceItemViewModel(trace);
            _stepItems[trace] = item;
            Steps.Add(item);
        }

        item.UpdateFromTrace();

        var attempt = trace.MaxAttempts > 1 ? $" (attempt {trace.Attempt}/{trace.MaxAttempts})" : "";
        Log($"Step {trace.Name}: {trace.Status}{attempt}");
    }

    private void HandleRunCompleted(WorkflowRunTrace trace)
    {
        Log($"Run {trace.RunId:N} {trace.Result} in {trace.Duration.TotalSeconds:0.0}s" +
            (trace.Fault is null ? "" : $" — {trace.Fault.Reason}"));
        RunElapsedText = $"{trace.Duration.TotalSeconds:0.0}s";
        RefreshElapsed();
    }

    private void HandleSessionSaved(SessionSavedEventArgs e)
    {
        LastSessionText = e.Success
            ? $"Session {e.Record.SessionId:N} saved to pending ({e.Record.Result}, {e.Record.Steps.Count} steps)"
            : $"Session save FAILED: {e.Error}";
        Log(LastSessionText);
    }

    private void RefreshElapsed()
    {
        if (_workflow.State == WorkstationState.Running)
        {
            RunElapsedText = $"{(DateTimeOffset.UtcNow - _runStartedAt).TotalSeconds:0.0}s";
        }

        foreach (var item in Steps)
        {
            if (item.IsRunning)
            {
                item.RefreshElapsed();
            }
        }
    }

    private void Log(string line)
    {
        RunLog.Add($"{DateTime.Now:HH:mm:ss.fff}  {line}");
        while (RunLog.Count > MaxLogLines)
        {
            RunLog.RemoveAt(0);
        }
    }

    /// <summary>Workflow events arrive on the run thread; WPF collections need the UI thread.</summary>
    private static void OnUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action);
        }
    }
}
