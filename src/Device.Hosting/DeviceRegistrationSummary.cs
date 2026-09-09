namespace Device.Hosting;

/// <summary>Lightweight registration summary for station shells (avoids UI depending on Device.Server).</summary>
public interface IDeviceRegistrationSummary
{
    int Count { get; }
}

public sealed class DeviceRegistrationSummary(int count) : IDeviceRegistrationSummary
{
    public int Count { get; } = count;
}
