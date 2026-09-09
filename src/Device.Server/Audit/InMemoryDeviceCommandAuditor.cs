using Device.Contracts.Common;
using System.Collections.Concurrent;

namespace Device.Server.Audit;

public sealed class InMemoryDeviceCommandAuditor : IDeviceCommandAuditor
{
    private readonly ConcurrentQueue<DeviceCommandAuditEntry> _entries = new();
    private readonly int _capacity;

    public InMemoryDeviceCommandAuditor(int capacity = 200)
    {
        _capacity = Math.Max(1, capacity);
    }

    public void Record(DeviceCommandAuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Enqueue(entry);
        while (_entries.Count > _capacity && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<DeviceCommandAuditEntry> GetRecent(int maxCount = 200)
    {
        var take = Math.Clamp(maxCount, 1, _capacity);
        return _entries.Reverse().Take(take).ToArray();
    }
}
