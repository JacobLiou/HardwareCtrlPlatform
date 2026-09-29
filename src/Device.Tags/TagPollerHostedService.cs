using Device.Client;
using Device.Contracts.Tags;
using Device.Server.Registry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Device.Tags;

/// <summary>
/// Periodic uplink poller. Failures publish Bad quality; does not crash the host.
/// </summary>
public sealed class TagPollerHostedService(
    ITagRegistry registry,
    ITagStore store,
    IUdlServerClient client,
    IReadOnlyList<DeviceDefinition> devices,
    ILogger<TagPollerHostedService> logger) : BackgroundService
{
    private readonly InMemoryTagStore? _memoryStore = store as InMemoryTagStore;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var readable = registry.Definitions
            .Where(d => d.Access is TagAccess.Read or TagAccess.ReadWrite)
            .ToArray();

        if (readable.Length == 0)
        {
            logger.LogInformation("Tag poller idle: no readable tags configured.");
            return;
        }

        // Seed Unknown snapshots so UI has rows immediately.
        var now = DateTimeOffset.UtcNow;
        foreach (var definition in readable)
        {
            if (!store.TryGet(definition.TagId, out _))
            {
                store.Publish(new TagSnapshot(
                    definition.TagId,
                    null,
                    definition.Unit,
                    TagQuality.Unknown,
                    now,
                    "Waiting for first poll."));
            }
        }

        var nextDue = readable.ToDictionary(
            d => d.TagId,
            _ => DateTimeOffset.UtcNow,
            StringComparer.OrdinalIgnoreCase);

        while (!stoppingToken.IsCancellationRequested)
        {
            var tick = DateTimeOffset.UtcNow;
            foreach (var definition in readable)
            {
                if (tick < nextDue[definition.TagId])
                {
                    continue;
                }

                try
                {
                    await CapabilityTagAdapter
                        .PollAsync(definition, client, devices, store, stoppingToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Tag poll failed for {TagId}", definition.TagId);
                    store.Publish(new TagSnapshot(
                        definition.TagId,
                        null,
                        definition.Unit,
                        TagQuality.Bad,
                        DateTimeOffset.UtcNow,
                        ex.Message));
                }

                nextDue[definition.TagId] = DateTimeOffset.UtcNow.AddMilliseconds(definition.PeriodMs);
            }

            _memoryStore?.ApplyStale(
                DateTimeOffset.UtcNow,
                tagId =>
                {
                    var def = readable.FirstOrDefault(d =>
                        string.Equals(d.TagId, tagId, StringComparison.OrdinalIgnoreCase));
                    var ms = def?.StaleAfterMs ?? 1000;
                    return TimeSpan.FromMilliseconds(ms);
                });

            try
            {
                await Task.Delay(50, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
