using Device.Contracts.Tags;

namespace Device.Tags.Tests;

public sealed class InMemoryTagStoreTests
{
    [Fact]
    public void Publish_and_TryGet_round_trip()
    {
        var store = new InMemoryTagStore();
        TagSnapshot? changed = null;
        store.Changed += s => changed = s;

        var snap = new TagSnapshot("PM-01.Power", -10.0, "dBm", TagQuality.Good, DateTimeOffset.UtcNow);
        store.Publish(snap);

        Assert.True(store.TryGet("PM-01.Power", out var got));
        Assert.Equal(-10.0, got!.Value);
        Assert.Equal(TagQuality.Good, got.Quality);
        Assert.Same(snap, changed);
        Assert.Single(store.Snapshot());
    }

    [Fact]
    public void ApplyStale_marks_old_Good_as_Stale()
    {
        var store = new InMemoryTagStore();
        var past = DateTimeOffset.UtcNow.AddSeconds(-5);
        store.Publish(new TagSnapshot("T1", 1, null, TagQuality.Good, past));

        store.ApplyStale(DateTimeOffset.UtcNow, _ => TimeSpan.FromSeconds(1));

        Assert.True(store.TryGet("T1", out var got));
        Assert.Equal(TagQuality.Stale, got!.Quality);
    }
}
