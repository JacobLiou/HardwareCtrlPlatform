using Device.Contracts.Tags;
using System.Collections.Concurrent;

namespace Device.Tags;

public sealed class InMemoryTagStore : ITagStore
{
    private readonly ConcurrentDictionary<string, TagSnapshot> _tags =
        new(StringComparer.OrdinalIgnoreCase);

    public event Action<TagSnapshot>? Changed;

    public IReadOnlyList<TagSnapshot> Snapshot() =>
        _tags.Values.OrderBy(t => t.TagId, StringComparer.OrdinalIgnoreCase).ToArray();

    public bool TryGet(string tagId, out TagSnapshot? value)
    {
        if (_tags.TryGetValue(tagId, out var found))
        {
            value = found;
            return true;
        }

        value = null;
        return false;
    }

    public void Publish(TagSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.TagId);

        _tags[snapshot.TagId] = snapshot;
        Changed?.Invoke(snapshot);
    }

    /// <summary>
    /// Marks Good snapshots as Stale when older than <paramref name="staleAfter"/> since UpdatedUtc.
    /// </summary>
    public void ApplyStale(DateTimeOffset nowUtc, Func<string, TimeSpan> staleAfterForTag)
    {
        ArgumentNullException.ThrowIfNull(staleAfterForTag);

        foreach (var pair in _tags)
        {
            var current = pair.Value;
            if (current.Quality != TagQuality.Good)
            {
                continue;
            }

            var age = nowUtc - current.UpdatedUtc;
            if (age <= staleAfterForTag(current.TagId))
            {
                continue;
            }

            var stale = current with
            {
                Quality = TagQuality.Stale,
                Message = current.Message ?? "Stale: no successful update within threshold."
            };
            _tags[pair.Key] = stale;
            Changed?.Invoke(stale);
        }
    }
}
