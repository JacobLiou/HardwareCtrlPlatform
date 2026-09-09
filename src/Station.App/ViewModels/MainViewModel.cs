using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Device.Hosting;
using Microsoft.Extensions.Configuration;
using Station.Workflow;

namespace Station.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IStationWorkflow _workflow;

    public MainViewModel(
        IStationWorkflow workflow,
        IConfiguration configuration,
        IDeviceRegistrationSummary devices)
    {
        _workflow = workflow;
        _workflow.StateChanged += (_, _) =>
        {
            // Marshal to UI if needed; Host may already be on UI thread for template.
            RefreshState();
        };

        Title = configuration["Station:DisplayName"] ?? "Station Template";
        DevicesText = $"Devices registered: {devices.Count}";
        RefreshState();
    }

    [ObservableProperty]
    private string _title = "Station Template";

    [ObservableProperty]
    private string _stateText = WorkstationState.Idle.ToString();

    [ObservableProperty]
    private string _faultText = "";

    [ObservableProperty]
    private string _devicesText = "Devices registered: 0";

    [ObservableProperty]
    private bool _canReset;

    [RelayCommand]
    private async Task StartAsync()
    {
        await _workflow.StartAsync();
        RefreshState();
    }

    [RelayCommand]
    private async Task AbortAsync()
    {
        await _workflow.AbortAsync();
        RefreshState();
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private async Task ResetAsync()
    {
        await _workflow.ResetAsync();
        RefreshState();
    }

    private void RefreshState()
    {
        StateText = _workflow.State.ToString();
        FaultText = _workflow.FaultInfo is null
            ? ""
            : $"Fault: {_workflow.FaultInfo.Reason}";
        CanReset = _workflow.State == WorkstationState.Fault;
        ResetCommand.NotifyCanExecuteChanged();
    }
}
