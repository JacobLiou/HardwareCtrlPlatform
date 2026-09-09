using Device.Server.Registry;

namespace Device.Hosting;

public sealed class DevicePlatformOptions
{
    public const string SectionName = "Devices";

    public List<DeviceEntryOptions> Entries { get; set; } = [];

    public IReadOnlyList<DeviceDefinition> ToDefinitions()
    {
        var list = new List<DeviceDefinition>(Entries.Count);
        foreach (var entry in Entries)
        {
            list.Add(entry.ToDefinition());
        }

        return list;
    }
}

public sealed class DeviceEntryOptions
{
    public string DeviceId { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public string Provider { get; set; } = nameof(DeviceProviderKind.Simulator);
    public string DriverName { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? ResourceId { get; set; }

    public DeviceDefinition ToDefinition()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
        {
            throw new InvalidOperationException("Devices:Entries DeviceId is required.");
        }

        if (string.IsNullOrWhiteSpace(DeviceType))
        {
            throw new InvalidOperationException($"Devices:Entries DeviceType is required for '{DeviceId}'.");
        }

        if (string.IsNullOrWhiteSpace(DriverName))
        {
            throw new InvalidOperationException($"Devices:Entries DriverName is required for '{DeviceId}'.");
        }

        if (!Enum.TryParse<DeviceProviderKind>(Provider, ignoreCase: true, out var kind))
        {
            throw new InvalidOperationException(
                $"Devices:Entries Provider '{Provider}' for '{DeviceId}' is not a known DeviceProviderKind.");
        }

        return new DeviceDefinition(
            DeviceId.Trim(),
            DeviceType.Trim(),
            kind,
            DriverName.Trim(),
            string.IsNullOrWhiteSpace(DisplayName) ? null : DisplayName.Trim(),
            string.IsNullOrWhiteSpace(ResourceId) ? null : ResourceId.Trim());
    }
}
