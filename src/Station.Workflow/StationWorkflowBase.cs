namespace Station.Workflow;

/// <summary>
/// Base station workflow: cooperative abort, run/step timeout, Fault + Reset,
/// start interlocks, and observable step / run traces.
/// Subclasses implement <see cref="ExecuteAsync"/>.
/// </summary>
public abstract class StationWorkflowBase : IStationWorkflow
{
    private readonly object _gate = new();
    private readonly List<IWorkflowInterlock> _interlocks = new();
    private readonly List<WorkflowStepTrace> _stepTraces = new();
    private CancellationTokenSource? _abortCts;
    private Task? _runTask;
    private volatile bool _abortRequested;
    private DateTimeOffset _runStartedAtUtc;

    protected StationWorkflowBase(
        WorkflowRunOptions? options = null,
        IEnumerable<IWorkflowInterlock>? interlocks = null)
    {
        Options = options ?? WorkflowRunOptions.Default;
        Steps = new WorkflowStepRunner(Options);
        Steps.StepTraceChanged += OnRunnerStepTraceChanged;
        if (interlocks is not null)
        {
            _interlocks.AddRange(interlocks);
        }
    }

    public WorkstationState State { get; private set; } = WorkstationState.Idle;

    public WorkstationFaultInfo? FaultInfo { get; private set; }

    /// <summary>Id of the current (or last) run. Changes on every successful Start.</summary>
    public Guid RunId { get; private set; } = Guid.Empty;

    /// <summary>Most recently started step of the current / last run.</summary>
    public WorkflowStepTrace? CurrentStep { get; private set; }

    /// <summary>Snapshot copy of the current / last run's step traces.</summary>
    public IReadOnlyList<WorkflowStepTrace> StepTraces
    {
        get
        {
            lock (_gate)
            {
                return _stepTraces.ToArray();
            }
        }
    }

    public event EventHandler<WorkstationState>? StateChanged;

    /// <summary>Forwarded from the step runner; also raised for step start / pass / fail / abort.</summary>
    public event EventHandler<WorkflowStepTrace>? StepTraceChanged;

    /// <summary>Raised once per run, after the run reaches Idle / Fault. Handlers should be quick.</summary>
    public event EventHandler<WorkflowRunTrace>? RunCompleted;

    protected WorkflowRunOptions Options { get; }

    protected WorkflowStepRunner Steps { get; }

    /// <summary>Station process body. Prefer calling <see cref="Steps"/> for timed/retryable work.</summary>
    protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CancellationToken runToken;
        lock (_gate)
        {
            if (State != WorkstationState.Idle
                || !StateTransitionGuard.CanTransition(State, WorkstationState.Running))
            {
                throw new InvalidOperationException($"Cannot start from state {State}.");
            }

            if (_runTask is { IsCompleted: false })
            {
                throw new InvalidOperationException("A workflow run is already in progress.");
            }

            RejectIfInterlocksNotSatisfied_NoLock();

            _abortRequested = false;
            FaultInfo = null;
            RunId = Guid.NewGuid();
            _runStartedAtUtc = DateTimeOffset.UtcNow;
            _stepTraces.Clear();
            CurrentStep = null;
            _abortCts?.Dispose();
            _abortCts = new CancellationTokenSource();

            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abortCts.Token);
            if (Options.RunTimeout is { } runTimeout && runTimeout > TimeSpan.Zero)
            {
                linked.CancelAfter(runTimeout);
            }

            runToken = linked.Token;
            SetState_NoLock(WorkstationState.Running);
            _runTask = RunCoreAsync(linked, runToken);
        }

        return _runTask;
    }

    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Task? run;
        lock (_gate)
        {
            if (State == WorkstationState.Idle)
            {
                return;
            }

            if (State == WorkstationState.Fault)
            {
                throw new InvalidOperationException("Cannot abort from Fault; call ResetAsync instead.");
            }

            _abortRequested = true;
            _abortCts?.Cancel();
            run = _runTask;
        }

        if (run is not null)
        {
            try
            {
                await run.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // RunCoreAsync records Fault/Idle; swallow to keep Abort idempotent for callers.
            }
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (State != WorkstationState.Fault
                || !StateTransitionGuard.CanTransition(State, WorkstationState.Idle))
            {
                throw new InvalidOperationException($"Cannot reset from state {State}.");
            }

            FaultInfo = null;
            _abortRequested = false;
            SetState_NoLock(WorkstationState.Idle);
        }

        return Task.CompletedTask;
    }

    private async Task RunCoreAsync(CancellationTokenSource linkedCts, CancellationToken runToken)
    {
        WorkflowRunTrace runTrace;
        try
        {
            await ExecuteAsync(runToken).ConfigureAwait(false);
            runTrace = EndRun();
        }
        catch (OperationCanceledException) when (_abortRequested)
        {
            runTrace = EndRun();
        }
        catch (OperationCanceledException oce)
        {
            runTrace = EnterFault($"Run cancelled or timed out: {oce.Message}", oce);
        }
        catch (TimeoutException tex)
        {
            runTrace = EnterFault(tex.Message, tex);
        }
        catch (Exception ex)
        {
            runTrace = EnterFault(ex.Message, ex);
        }
        finally
        {
            linkedCts.Dispose();
        }

        RunCompleted?.Invoke(this, runTrace);
    }

    private WorkflowRunTrace EndRun()
    {
        lock (_gate)
        {
            var result = _abortRequested ? WorkflowRunResult.Aborted : WorkflowRunResult.Completed;

            FaultInfo = null;
            if (State == WorkstationState.Running)
            {
                SetState_NoLock(WorkstationState.Idle);
            }

            return BuildRunTrace_NoLock(result);
        }
    }

    private WorkflowRunTrace EnterFault(string reason, Exception? ex)
    {
        lock (_gate)
        {
            // User Abort wins: cooperative cancel ends Idle, not Fault.
            if (_abortRequested)
            {
                FaultInfo = null;
                SetState_NoLock(WorkstationState.Idle);
                return BuildRunTrace_NoLock(WorkflowRunResult.Aborted);
            }

            if (State is WorkstationState.Running or WorkstationState.Fault)
            {
                FaultInfo = new WorkstationFaultInfo(
                    reason,
                    DateTimeOffset.UtcNow,
                    ex?.GetType().FullName);
                SetState_NoLock(WorkstationState.Fault);
            }

            return BuildRunTrace_NoLock(WorkflowRunResult.Faulted);
        }
    }

    private WorkflowRunTrace BuildRunTrace_NoLock(WorkflowRunResult result) =>
        new(RunId, _runStartedAtUtc, DateTimeOffset.UtcNow, result, FaultInfo, _stepTraces.ToArray());

    private void OnRunnerStepTraceChanged(object? sender, WorkflowStepTrace trace)
    {
        lock (_gate)
        {
            if (!_stepTraces.Contains(trace))
            {
                _stepTraces.Add(trace);
            }

            if (trace.Status == WorkflowStepStatus.Running)
            {
                CurrentStep = trace;
            }
        }

        StepTraceChanged?.Invoke(this, trace);
    }

    private void RejectIfInterlocksNotSatisfied_NoLock()
    {
        if (_interlocks.Count == 0)
        {
            return;
        }

        List<string>? failures = null;
        foreach (var interlock in _interlocks)
        {
            if (interlock.IsSatisfied(out var reason))
            {
                continue;
            }

            (failures ??= new List<string>()).Add(
                string.IsNullOrWhiteSpace(reason) ? interlock.Name : $"{interlock.Name}: {reason}");
        }

        if (failures is not null)
        {
            throw new WorkflowStartRejectedException(failures);
        }
    }

    private void SetState_NoLock(WorkstationState next)
    {
        if (State == next)
        {
            return;
        }

        State = next;
        StateChanged?.Invoke(this, next);
    }
}
