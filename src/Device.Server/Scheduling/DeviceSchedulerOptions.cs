namespace Device.Server.Scheduling;

/// <summary>
/// Options for <see cref="DeviceCommandExecutor"/>.
/// Default policy: serial per ResourceId, parallel across different ResourceIds.
/// </summary>
public sealed class DeviceSchedulerOptions
{
    /// <summary>
    /// Reserved for a future per-resource wait timeout (YAGNI until needed).
    /// </summary>
    public int? WaitTimeoutMs { get; set; }
}
