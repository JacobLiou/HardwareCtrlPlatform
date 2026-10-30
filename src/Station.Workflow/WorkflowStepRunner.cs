namespace Station.Workflow;

/// <summary>
/// Runs a named step with cooperative cancel, optional timeout, and bounded retry (BCL only).
/// Raises <see cref="StepTraceChanged"/> on every status change so hosts can bind step traces.
/// </summary>
public sealed class WorkflowStepRunner
{
    private readonly WorkflowRunOptions _defaults;

    /// <summary>Raised whenever the step's status / attempt / duration changes.</summary>
    public event EventHandler<WorkflowStepTrace>? StepTraceChanged;

    public WorkflowStepRunner(WorkflowRunOptions? defaults = null)
    {
        _defaults = defaults ?? WorkflowRunOptions.Default;
    }

    private void RaiseTraceChanged(WorkflowStepTrace trace) => StepTraceChanged?.Invoke(this, trace);

    public async Task RunAsync(
        string name,
        Func<CancellationToken, Task> action,
        WorkflowStepOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(action);

        var maxAttempts = Math.Max(1, options?.MaxAttempts ?? _defaults.DefaultMaxAttempts);
        var timeout = options?.Timeout ?? _defaults.DefaultStepTimeout;
        var retryDelay = options?.RetryDelay ?? _defaults.RetryDelay;

        var trace = new WorkflowStepTrace
        {
            Name = name,
            MaxAttempts = maxAttempts,
            Status = WorkflowStepStatus.Pending
        };

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout > TimeSpan.Zero)
            {
                stepCts.CancelAfter(timeout);
            }

            trace.Attempt = attempt;
            trace.StartedAtUtc ??= DateTimeOffset.UtcNow;
            trace.Status = WorkflowStepStatus.Running;
            RaiseTraceChanged(trace);

            try
            {
                await action(stepCts.Token).ConfigureAwait(false);
                trace.Status = WorkflowStepStatus.Passed;
                trace.Duration = DateTimeOffset.UtcNow - trace.StartedAtUtc.Value;
                RaiseTraceChanged(trace);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                trace.Status = WorkflowStepStatus.Aborted;
                trace.Duration = DateTimeOffset.UtcNow - trace.StartedAtUtc.Value;
                RaiseTraceChanged(trace);
                throw;
            }
            catch (OperationCanceledException) when (stepCts.IsCancellationRequested)
            {
                last = new TimeoutException($"Step '{name}' timed out after {timeout}.");
                trace.Error = last.Message;
                if (attempt >= maxAttempts)
                {
                    trace.Status = WorkflowStepStatus.Failed;
                    trace.Duration = DateTimeOffset.UtcNow - trace.StartedAtUtc.Value;
                    RaiseTraceChanged(trace);
                    throw last;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                last = ex;
                trace.Error = ex.Message;
            }
            catch (Exception ex)
            {
                trace.Error = ex.Message;
                trace.Status = WorkflowStepStatus.Failed;
                trace.Duration = DateTimeOffset.UtcNow - trace.StartedAtUtc.Value;
                RaiseTraceChanged(trace);
                throw;
            }

            if (attempt < maxAttempts && retryDelay > TimeSpan.Zero)
            {
                await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw last ?? new InvalidOperationException($"Step '{name}' failed after {maxAttempts} attempts.");
    }
}
