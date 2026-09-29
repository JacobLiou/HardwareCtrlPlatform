namespace Device.Contracts.Tags;

public interface ITagStore
{
    IReadOnlyList<TagSnapshot> Snapshot();

    bool TryGet(string tagId, out TagSnapshot? value);

    /// <summary>Publish an uplink update (poller / adapters).</summary>
    void Publish(TagSnapshot snapshot);

    event Action<TagSnapshot>? Changed;
}
