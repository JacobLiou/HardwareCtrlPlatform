namespace Station.Workflow;

/// <summary>
/// Base station workflow: cooperative abort, run/step timeout, Fault + Reset.
/// Subclasses implement <see cref="ExecuteAsync"/>.
/// </summary>
public abstract class StationWorkflowBase : IStationWorkflow
{
    private readonly object _gate = new();
    private CancellationTokenSource? _abortCts;
    private Task? _runTask;
    private volatile bool _abortRequested;

    protected StationWorkflowBase(WorkflowRunOptions? options = null)
    {
        Options = options ?? WorkflowRunOptions.Default;
        Steps = new WorkflowStepRunner(Options);
    }

    public WorkstationState State { get; private set; } = WorkstationState.Idle;

    public WorkstationFaultInfo? FaultInfo { get; private set; }

    public event EventHandler<WorkstationState>? StateChanged;

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

            _abortRequested = false;
            FaultInfo = null;
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
        try
        {
            await ExecuteAsync(runToken).ConfigureAwait(false);

            lock (_gate)
            {
                if (_abortRequested)
                {
                    FaultInfo = null;
                    SetState_NoLock(WorkstationState.Idle);
                }
                else if (State == WorkstationState.Running)
                {
                    FaultInfo = null;
                    SetState_NoLock(WorkstationState.Idle);
                }
            }
        }
        catch (OperationCanceledException) when (_abortRequested)
        {
            lock (_gate)
            {
                FaultInfo = null;
                SetState_NoLock(WorkstationState.Idle);
            }
        }
        catch (OperationCanceledException oce)
        {
            EnterFault($"Run cancelled or timed out: {oce.Message}", oce);
        }
        catch (TimeoutException tex)
        {
            EnterFault(tex.Message, tex);
        }
        catch (Exception ex)
        {
            EnterFault(ex.Message, ex);
        }
        finally
        {
            linkedCts.Dispose();
        }
    }

    private void EnterFault(string reason, Exception? ex)
    {
        lock (_gate)
        {
            // User Abort wins: cooperative cancel ends Idle, not Fault.
            if (_abortRequested)
            {
                FaultInfo = null;
                SetState_NoLock(WorkstationState.Idle);
                return;
            }

            if (State is WorkstationState.Running or WorkstationState.Fault)
            {
                FaultInfo = new WorkstationFaultInfo(
                    reason,
                    DateTimeOffset.UtcNow,
                    ex?.GetType().FullName);
                SetState_NoLock(WorkstationState.Fault);
            }
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
