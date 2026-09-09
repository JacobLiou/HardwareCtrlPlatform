namespace Station.Workflow;

/// <summary>Fault details when <see cref="WorkstationState.Fault"/>.</summary>
public sealed record WorkstationFaultInfo(
    string Reason,
    DateTimeOffset OccurredAt,
    string? ExceptionType = null);
