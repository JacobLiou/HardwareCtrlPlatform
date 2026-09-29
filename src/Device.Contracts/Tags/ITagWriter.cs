using Device.Contracts.Common;

namespace Device.Contracts.Tags;

public interface ITagWriter
{
    Task<DeviceResult> WriteAsync(string tagId, object? value, CancellationToken cancellationToken);
}
