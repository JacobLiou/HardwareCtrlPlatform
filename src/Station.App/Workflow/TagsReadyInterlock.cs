using Device.Contracts.Tags;
using Station.Workflow;

namespace Station.App.Workflow;

/// <summary>
/// Sample interlock: Start is rejected until at least one tag reports Good quality
/// (i.e. the poller sees live device data). Copy this pattern for door / air / safety interlocks.
/// </summary>
public sealed class TagsReadyInterlock(ITagStore tagStore) : IWorkflowInterlock
{
    private const int MinimumGoodTags = 1;

    public string Name => "TagsReady";

    public bool IsSatisfied(out string? reason)
    {
        var good = tagStore.Snapshot().Count(t => t.Quality == TagQuality.Good);
        if (good >= MinimumGoodTags)
        {
            reason = null;
            return true;
        }

        reason = $"no Good-quality tags ({good}/{MinimumGoodTags})";
        return false;
    }
}
