using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Device.Client;
using Device.Contracts.Capabilities;
using Device.Contracts.Common;
using Device.Contracts.Tags;
using Device.Server.Registry;
using Device.Tags;

namespace Station.App.ViewModels;

public sealed class DeviceListItem
{
    public DeviceListItem(DeviceDefinition definition)
    {
        Definition = definition;
    }

    public DeviceDefinition Definition { get; }

    public string Title =>
        string.IsNullOrWhiteSpace(Definition.DisplayName)
            ? $"{Definition.DeviceId} ({Definition.DeviceType})"
            : $"{Definition.DisplayName} [{Definition.DeviceId}]";

    public override string ToString() => Title;
}

public partial class LiveTagRow : ObservableObject
{
    [ObservableProperty]
    private string _tagId = "";

    [ObservableProperty]
    private string _valueText = "-";

    [ObservableProperty]
    private string _unit = "";

    [ObservableProperty]
    private string _quality = "Unknown";

    [ObservableProperty]
    private string _updated = "";

    [ObservableProperty]
    private bool _canWrite;
}

public partial class DeviceDebugViewModel : ObservableObject
{
    private readonly IUdlServerClient _client;
    private readonly IDeviceCommandAuditor? _auditor;
    private readonly ITagStore? _tagStore;
    private readonly ITagWriter? _tagWriter;
    private readonly ITagRegistry? _tagRegistry;
    private readonly DispatcherTimer? _tagRefreshTimer;
    private IDevice? _activeDevice;
    private string? _panelKind;
    private readonly StringBuilder _log = new();

    public DeviceDebugViewModel(
        IUdlServerClient client,
        IReadOnlyList<DeviceDefinition> definitions,
        IDeviceCommandAuditor? auditor = null,
        ITagStore? tagStore = null,
        ITagWriter? tagWriter = null,
        ITagRegistry? tagRegistry = null)
    {
        _client = client;
        _auditor = auditor;
        _tagStore = tagStore;
        _tagWriter = tagWriter;
        _tagRegistry = tagRegistry;

        foreach (var definition in definitions)
        {
            Devices.Add(new DeviceListItem(definition));
        }

        if (Devices.Count > 0)
        {
            SelectedDevice = Devices[0];
        }
        else
        {
            AppendLog("No devices in Devices:Entries. Edit appsettings.json.");
        }

        RebuildLiveTagRows();
        if (_tagStore is not null)
        {
            _tagStore.Changed += OnTagChanged;
            _tagRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _tagRefreshTimer.Tick += (_, _) => RefreshLiveTagsFromStore();
            _tagRefreshTimer.Start();
        }
    }

    public ObservableCollection<DeviceListItem> Devices { get; } = [];

    public ObservableCollection<LiveTagRow> LiveTags { get; } = [];

    [ObservableProperty]
    private DeviceListItem? _selectedDevice;

    [ObservableProperty]
    private LiveTagRow? _selectedLiveTag;

    [ObservableProperty]
    private string _tagWriteValue = "";

    [ObservableProperty]
    private string _headerText = "Select a device";

    [ObservableProperty]
    private string _healthText = "-";

    [ObservableProperty]
    private string _panelHint = "";

    [ObservableProperty]
    private bool _isPowerMeter;

    [ObservableProperty]
    private bool _isLaser;

    [ObservableProperty]
    private bool _isOpticalSwitch;

    [ObservableProperty]
    private bool _hasMappedPanel;

    [ObservableProperty]
    private bool _hasLiveTags;

    [ObservableProperty]
    private int _channel;

    [ObservableProperty]
    private double _wavelengthNm = 1550;

    [ObservableProperty]
    private bool _laserOutputEnabled;

    [ObservableProperty]
    private int _inputPort = 1;

    [ObservableProperty]
    private int _outputPort = 2;

    [ObservableProperty]
    private string _uplinkValue = "-";

    [ObservableProperty]
    private string _logText = "";

    [ObservableProperty]
    private string _auditText = "";

    partial void OnSelectedDeviceChanged(DeviceListItem? value)
    {
        _activeDevice = null;
        _panelKind = null;
        IsPowerMeter = IsLaser = IsOpticalSwitch = HasMappedPanel = false;
        UplinkValue = "-";
        HealthText = "-";

        if (value is null)
        {
            HeaderText = "Select a device";
            PanelHint = "";
            return;
        }

        var d = value.Definition;
        HeaderText = $"{d.DeviceId} | {d.DeviceType} | {d.Provider} | {d.DriverName}";
        var identity = new DeviceIdentity(d.DeviceId, d.DeviceType, d.DisplayName);

        if (!CapabilityProxyFactory.TryCreate(_client, identity, out var device, out var panelKind)
            || device is null
            || panelKind is null)
        {
            PanelHint = $"No capability panel for DeviceType '{d.DeviceType}'. Health-only via raw device is unavailable in console; add a proxy mapping.";
            AppendLog($"Selected {d.DeviceId}: unmapped type {d.DeviceType}");
            return;
        }

        _activeDevice = device;
        _panelKind = panelKind;
        HasMappedPanel = true;
        IsPowerMeter = panelKind == CapabilityProxyFactory.OpticalPowerMeter;
        IsLaser = panelKind == CapabilityProxyFactory.LaserSource;
        IsOpticalSwitch = panelKind == CapabilityProxyFactory.OpticalSwitch;
        PanelHint = $"Panel: {panelKind}";
        AppendLog($"Selected {d.DeviceId} → {panelKind}");
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (_activeDevice is not IDeviceConnection connection)
        {
            AppendLog("Connect: device does not implement IDeviceConnection");
            return;
        }

        var result = await connection.ConnectAsync(CancellationToken.None);
        AppendLog(result.Success ? "Connect OK" : $"Connect FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
        if (result.Success)
        {
            await RefreshHealthAsync();
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        if (_activeDevice is not IDeviceConnection connection)
        {
            AppendLog("Disconnect: device does not implement IDeviceConnection");
            return;
        }

        var result = await connection.DisconnectAsync(CancellationToken.None);
        AppendLog(result.Success ? "Disconnect OK" : $"Disconnect FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
        if (result.Success)
        {
            await RefreshHealthAsync();
        }
    }

    [RelayCommand]
    private async Task ReconnectAsync()
    {
        if (_activeDevice is not IDeviceConnection connection)
        {
            AppendLog("Reconnect: device does not implement IDeviceConnection");
            return;
        }

        var result = await connection.ReconnectAsync(CancellationToken.None);
        AppendLog(result.Success ? "Reconnect OK" : $"Reconnect FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
        if (result.Success)
        {
            await RefreshHealthAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshHealthAsync()
    {
        if (_activeDevice is null)
        {
            AppendLog("Health: no active device");
            return;
        }

        var result = await _activeDevice.GetHealthAsync(CancellationToken.None);
        if (result.Success && result.Data is not null)
        {
            HealthText = $"{result.Data.State} | Healthy={result.Data.IsHealthy} | {result.Data.Message}";
            AppendLog($"Health OK: {HealthText}");
        }
        else
        {
            HealthText = $"FAIL {result.ErrorCode}: {result.Message}";
            AppendLog($"Health FAIL: {HealthText}");
        }

        RefreshAuditPanel();
    }

    [RelayCommand]
    private async Task ReadPowerAsync()
    {
        if (_activeDevice is not IOpticalPowerMeter meter)
        {
            AppendLog("ReadPower: not a power meter");
            return;
        }

        var result = await meter.ReadPowerAsync(Channel, CancellationToken.None);
        if (result.Success && result.Data is not null)
        {
            UplinkValue = $"{result.Data.Value} {result.Data.Unit} @ ch{Channel}";
            AppendLog($"ReadPower OK: {UplinkValue}");
        }
        else
        {
            UplinkValue = $"FAIL {result.ErrorCode}";
            AppendLog($"ReadPower FAIL: {result.ErrorCode} {result.Message}");
        }

        RefreshAuditPanel();
    }

    [RelayCommand]
    private async Task SetMeterWavelengthAsync()
    {
        if (_activeDevice is not IOpticalPowerMeter meter)
        {
            AppendLog("SetWavelength: not a power meter");
            return;
        }

        var result = await meter.SetWavelengthAsync(WavelengthNm, CancellationToken.None);
        AppendLog(result.Success
            ? $"SetWavelength OK: {WavelengthNm} nm"
            : $"SetWavelength FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
    }

    [RelayCommand]
    private async Task SetLaserWavelengthAsync()
    {
        if (_activeDevice is not ILaserSource laser)
        {
            AppendLog("Laser SetWavelength: not a laser");
            return;
        }

        var result = await laser.SetWavelengthAsync(WavelengthNm, CancellationToken.None);
        AppendLog(result.Success
            ? $"Laser λ OK: {WavelengthNm} nm"
            : $"Laser λ FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
    }

    [RelayCommand]
    private async Task SetLaserOutputAsync()
    {
        if (_activeDevice is not ILaserSource laser)
        {
            AppendLog("Laser Output: not a laser");
            return;
        }

        var result = await laser.SetOutputAsync(LaserOutputEnabled, CancellationToken.None);
        AppendLog(result.Success
            ? $"Laser Output OK: {LaserOutputEnabled}"
            : $"Laser Output FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
    }

    [RelayCommand]
    private async Task SwitchToAsync()
    {
        if (_activeDevice is not IOpticalSwitch opticalSwitch)
        {
            AppendLog("SwitchTo: not an optical switch");
            return;
        }

        var result = await opticalSwitch.SwitchToAsync(InputPort, OutputPort, CancellationToken.None);
        AppendLog(result.Success
            ? $"SwitchTo OK: {InputPort}→{OutputPort}"
            : $"SwitchTo FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
    }

    [RelayCommand]
    private void RefreshAudit() => RefreshAuditPanel();

    [RelayCommand]
    private void RefreshLiveTags() => RefreshLiveTagsFromStore();

    [RelayCommand]
    private async Task WriteSelectedTagAsync()
    {
        if (_tagWriter is null || SelectedLiveTag is null || !SelectedLiveTag.CanWrite)
        {
            AppendLog("Tag write: no writable tag selected");
            return;
        }

        object? value = TagWriteValue;
        if (bool.TryParse(TagWriteValue, out var b))
        {
            value = b;
        }
        else if (double.TryParse(TagWriteValue, System.Globalization.NumberStyles.Float,
                     System.Globalization.CultureInfo.InvariantCulture, out var d))
        {
            value = d;
        }

        var result = await _tagWriter.WriteAsync(SelectedLiveTag.TagId, value, CancellationToken.None);
        AppendLog(result.Success
            ? $"Tag write OK: {SelectedLiveTag.TagId}={TagWriteValue}"
            : $"Tag write FAIL: {result.ErrorCode} {result.Message}");
        RefreshAuditPanel();
    }

    private void OnTagChanged(TagSnapshot _)
    {
        // DispatcherTimer refreshes UI; avoid cross-thread ObservableCollection edits here.
    }

    private void RebuildLiveTagRows()
    {
        LiveTags.Clear();
        if (_tagRegistry is null)
        {
            HasLiveTags = false;
            return;
        }

        foreach (var def in _tagRegistry.Definitions)
        {
            LiveTags.Add(new LiveTagRow
            {
                TagId = def.TagId,
                CanWrite = def.Access is TagAccess.Write or TagAccess.ReadWrite,
                Unit = def.Unit ?? ""
            });
        }

        HasLiveTags = LiveTags.Count > 0;
        RefreshLiveTagsFromStore();
    }

    private void RefreshLiveTagsFromStore()
    {
        if (_tagStore is null)
        {
            return;
        }

        var snaps = _tagStore.Snapshot().ToDictionary(s => s.TagId, StringComparer.OrdinalIgnoreCase);
        foreach (var row in LiveTags)
        {
            if (!snaps.TryGetValue(row.TagId, out var snap))
            {
                continue;
            }

            row.ValueText = snap.Value?.ToString() ?? "-";
            row.Unit = snap.Unit ?? row.Unit;
            row.Quality = snap.Quality.ToString();
            row.Updated = snap.UpdatedUtc.ToLocalTime().ToString("HH:mm:ss.fff");
        }
    }

    private void RefreshAuditPanel()
    {
        if (_auditor is null)
        {
            AuditText = "(no auditor registered)";
            return;
        }

        var lines = _auditor.GetRecent(40)
            .Select(e =>
                $"{e.TimestampUtc:HH:mm:ss.fff} {e.DeviceId}/{e.ResourceId} {e.Capability}.{e.Operation} " +
                $"{(e.Success ? "OK" : e.ErrorCode.ToString())} {e.DurationMs:F1}ms {e.Message}");
        AuditText = string.Join(Environment.NewLine, lines);
    }

    private void AppendLog(string line)
    {
        _log.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {line}{Environment.NewLine}");
        if (_log.Length > 8000)
        {
            _log.Length = 8000;
        }

        LogText = _log.ToString();
    }
}
