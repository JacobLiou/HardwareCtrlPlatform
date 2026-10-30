using Microsoft.Extensions.Options;
using Station.Workflow;

namespace Station.Session;

public sealed class SessionSavedEventArgs : EventArgs
{
    public required SessionRecord Record { get; init; }

    public string? FilePath { get; init; }

    public string? Error { get; init; }

    public bool Success => Error is null;
}

/// <summary>
/// Subscribes to <see cref="IStationWorkflow.RunCompleted"/> and persists one
/// <see cref="SessionRecord"/> per run (local-first, pending upload).
/// </summary>
public sealed class SessionRecorder
{
    private readonly ISessionStore _store;

    public SessionRecorder(
        IStationWorkflow workflow,
        ISessionStore store,
        IOptions<SessionOptions> options)
    {
        _store = store;
        Metadata = new SessionMetadata
        {
            StationId = options.Value.StationId,
            OperatorId = options.Value.OperatorId,
            RecipeName = options.Value.RecipeName,
            RecipeVersion = options.Value.RecipeVersion
        };

        workflow.RunCompleted += OnRunCompleted;
    }

    /// <summary>Station / operator / recipe metadata for the next record. Set by the host shell.</summary>
    public SessionMetadata Metadata { get; set; }

    /// <summary>Optional provider invoked when a record is built (e.g. latest Good tag values).</summary>
    public Func<IReadOnlyDictionary<string, object?>?>? MeasurementsProvider { get; set; }

    /// <summary>Raised after each save attempt (check <see cref="SessionSavedEventArgs.Success"/>).</summary>
    public event EventHandler<SessionSavedEventArgs>? SessionSaved;

    /// <summary>The save triggered by the latest RunCompleted; await in tests.</summary>
    public Task? LastSave { get; private set; }

    public async Task SaveAsync(WorkflowRunTrace trace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trace);

        var record = BuildRecord(trace);
        try
        {
            var filePath = await _store.SaveAsync(record, cancellationToken).ConfigureAwait(false);
            SessionSaved?.Invoke(this, new SessionSavedEventArgs { Record = record, FilePath = filePath });
        }
        catch (Exception ex)
        {
            // Local save failures must never crash the station; surface them and keep running.
            SessionSaved?.Invoke(this, new SessionSavedEventArgs { Record = record, Error = ex.Message });
        }
    }

    private void OnRunCompleted(object? sender, WorkflowRunTrace trace) => LastSave = SaveAsync(trace);

    private SessionRecord BuildRecord(WorkflowRunTrace trace)
    {
        var steps = trace.Steps
            .Select(s => new SessionStepRecord(
                s.Name,
                s.Status.ToString(),
                s.Attempt,
                s.MaxAttempts,
                s.StartedAtUtc,
                s.Duration?.TotalMilliseconds,
                s.Error))
            .ToList();

        IReadOnlyDictionary<string, object?>? measurements = null;
        try
        {
            measurements = MeasurementsProvider?.Invoke();
        }
        catch
        {
            // Measurement capture is best-effort; a broken provider must not block session persistence.
        }

        return new SessionRecord(
            trace.RunId,
            Metadata.StationId,
            Metadata.OperatorId,
            Metadata.RecipeName,
            Metadata.RecipeVersion,
            trace.StartedAtUtc,
            trace.CompletedAtUtc,
            trace.Result.ToString(),
            trace.Fault?.Reason,
            steps,
            measurements);
    }
}
