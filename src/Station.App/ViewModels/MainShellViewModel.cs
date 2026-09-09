namespace Station.App.ViewModels;

/// <summary>Root shell holding Station + Devices debug tabs.</summary>
public sealed class MainShellViewModel
{
    public MainShellViewModel(MainViewModel station, DeviceDebugViewModel devices)
    {
        Station = station;
        Devices = devices;
    }

    public MainViewModel Station { get; }

    public DeviceDebugViewModel Devices { get; }
}
