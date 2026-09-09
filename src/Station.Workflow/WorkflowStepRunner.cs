namespace Station.Workflow;

/// <summary>
/// Runs a named step with cooperative cancel, optional timeout, and bounded retry (BCL only).
/// </summary>
public sealed class WorkflowStepRunner
{
    private readonly WorkflowRunOptions _defaults;

    public WorkflowStepRunner(WorkflowRunOptions? defaults = null)
    {
        _defaults = defaults ?? WorkflowRunOptions.Default;
    }

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

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout > TimeSpan.Zero)
            {
                stepCts.CancelAfter(timeout);
            }

            try
            {
                await action(stepCts.Token).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (stepCts.IsCancellationRequested)
            {
                last = new TimeoutException($"Step '{name}' timed out after {timeout}.");
                if (attempt >= maxAttempts)
                {
                    throw last;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                last = ex;
            }
            catch (Exception)
            {
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
