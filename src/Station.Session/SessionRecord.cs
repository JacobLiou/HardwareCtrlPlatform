namespace Station.Session;

/// <summary>Serialized snapshot of one step inside a session (no live references).</summary>
public sealed record SessionStepRecord(
    string Name,
    string Status,
    int Attempt,
    int MaxAttempts,
    DateTimeOffset? StartedAtUtc,
    double? DurationMs,
    string? Error);

/// <summary>
/// Local-first session record (P5 traceability minimal set): run id, station / operator / recipe,
/// outcome, step traces, and optional measurements. Saved to disk before any upload.
/// </summary>
public sealed record SessionRecord(
    Guid SessionId,
    string StationId,
    string? OperatorId,
    string? RecipeName,
    string? RecipeVersion,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    string Result,
    string? FaultReason,
    IReadOnlyList<SessionStepRecord> Steps,
    IReadOnlyDictionary<string, object?>? Measurements = null)
{
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;
}

/// <summary>Per-run station metadata filled by the host (operator / recipe come from outside the workflow).</summary>
public sealed record SessionMetadata
{
    public string StationId { get; init; } = "STATION";

    public string? OperatorId { get; init; }

    public string? RecipeName { get; init; }

    public string? RecipeVersion { get; init; }
}
