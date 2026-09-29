using Device.Contracts.Tags;

namespace Device.Tags;

public interface ITagRegistry
{
    IReadOnlyList<TagDefinition> Definitions { get; }

    bool TryGet(string tagId, out TagDefinition? definition);
}

public sealed class TagRegistry : ITagRegistry
{
    private readonly Dictionary<string, TagDefinition> _byId;

    public TagRegistry(IEnumerable<TagDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _byId = new Dictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in definitions)
        {
            CapabilityTagAdapter.EnsureSupported(definition);
            if (!_byId.TryAdd(definition.TagId, definition))
            {
                throw new InvalidOperationException($"Duplicate TagId '{definition.TagId}'.");
            }
        }

        Definitions = _byId.Values
            .OrderBy(d => d.TagId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<TagDefinition> Definitions { get; }

    public bool TryGet(string tagId, out TagDefinition? definition) =>
        _byId.TryGetValue(tagId, out definition);
}
