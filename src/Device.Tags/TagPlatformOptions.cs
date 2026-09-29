using Device.Contracts.Tags;

namespace Device.Tags;

public sealed class TagPlatformOptions
{
    public const string SectionName = "Tags";

    public int DefaultPeriodMs { get; set; } = 500;

    public List<TagEntryOptions> Entries { get; set; } = [];

    public IReadOnlyList<TagDefinition> ToDefinitions(
        Func<string, string?>? resolveDeviceType = null)
    {
        var list = new List<TagDefinition>(Entries.Count);
        foreach (var entry in Entries)
        {
            list.Add(entry.ToDefinition(DefaultPeriodMs, resolveDeviceType));
        }

        return list;
    }
}

public sealed class TagEntryOptions
{
    public string TagId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Access { get; set; } = nameof(TagAccess.Read);
    public string Capability { get; set; } = "";
    public string Operation { get; set; } = "";
    public Dictionary<string, object?> Args { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int? PeriodMs { get; set; }
    public int? StaleAfterMs { get; set; }
    public string? Unit { get; set; }

    public TagDefinition ToDefinition(int defaultPeriodMs, Func<string, string?>? resolveDeviceType)
    {
        if (string.IsNullOrWhiteSpace(TagId))
        {
            throw new InvalidOperationException("Tags:Entries TagId is required.");
        }

        if (string.IsNullOrWhiteSpace(DeviceId))
        {
            throw new InvalidOperationException($"Tags:Entries DeviceId is required for '{TagId}'.");
        }

        if (string.IsNullOrWhiteSpace(Capability))
        {
            throw new InvalidOperationException($"Tags:Entries Capability is required for '{TagId}'.");
        }

        if (string.IsNullOrWhiteSpace(Operation))
        {
            throw new InvalidOperationException($"Tags:Entries Operation is required for '{TagId}'.");
        }

        if (!Enum.TryParse<TagAccess>(Access, ignoreCase: true, out var access))
        {
            throw new InvalidOperationException(
                $"Tags:Entries Access '{Access}' for '{TagId}' is invalid.");
        }

        var period = PeriodMs is > 0 ? PeriodMs.Value : defaultPeriodMs;
        if (period <= 0)
        {
            period = 500;
        }

        var stale = StaleAfterMs is > 0 ? StaleAfterMs.Value : period * 2;

        return new TagDefinition
        {
            TagId = TagId.Trim(),
            DeviceId = DeviceId.Trim(),
            Access = access,
            Capability = Capability.Trim(),
            Operation = Operation.Trim(),
            Args = new Dictionary<string, object?>(Args, StringComparer.OrdinalIgnoreCase),
            PeriodMs = period,
            StaleAfterMs = stale,
            Unit = string.IsNullOrWhiteSpace(Unit) ? null : Unit.Trim(),
            DeviceType = resolveDeviceType?.Invoke(DeviceId.Trim())
        };
    }
}
