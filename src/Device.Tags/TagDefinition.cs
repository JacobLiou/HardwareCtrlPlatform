using Device.Contracts.Tags;

namespace Device.Tags;

/// <summary>Resolved tag binding from configuration.</summary>
public sealed class TagDefinition
{
    public required string TagId { get; init; }

    public required string DeviceId { get; init; }

    public required TagAccess Access { get; init; }

    public required string Capability { get; init; }

    public required string Operation { get; init; }

    public IReadOnlyDictionary<string, object?> Args { get; init; } =
        new Dictionary<string, object?>();

    public int PeriodMs { get; init; }

    public int StaleAfterMs { get; init; }

    public string? Unit { get; init; }

    public string? DeviceType { get; init; }
}
