using Device.Client;
using Device.Contracts.Common;
using Device.Contracts.Tags;
using Device.Server.Registry;

namespace Device.Tags;

public sealed class TagWriter(
    ITagRegistry registry,
    IUdlServerClient client,
    IReadOnlyList<DeviceDefinition> devices) : ITagWriter
{
    public Task<DeviceResult> WriteAsync(string tagId, object? value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagId);

        if (!registry.TryGet(tagId, out var definition) || definition is null)
        {
            return Task.FromResult(DeviceResult.Fail(
                DeviceErrorCode.NotFound,
                $"Tag '{tagId}' is not registered."));
        }

        return CapabilityTagAdapter.WriteAsync(definition, client, devices, value, cancellationToken);
    }
}
