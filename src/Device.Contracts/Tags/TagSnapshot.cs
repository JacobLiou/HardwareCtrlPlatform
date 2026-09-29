namespace Device.Contracts.Tags;

public sealed record TagSnapshot(
    string TagId,
    object? Value,
    string? Unit,
    TagQuality Quality,
    DateTimeOffset UpdatedUtc,
    string? Message = null);
