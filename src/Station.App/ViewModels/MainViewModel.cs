using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Station.Workflow;

namespace Station.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IStationWorkflow _workflow;

    public MainViewModel(IStationWorkflow workflow, IConfiguration configuration)
    {
        _workflow = workflow;
        Title = configuration["Station:DisplayName"] ?? "Station Template";
        RefreshState();
    }

    [ObservableProperty]
    private string _title = "Station Template";

    [ObservableProperty]
    private string _stateText = WorkstationState.Idle.ToString();

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

    private void RefreshState() => StateText = _workflow.State.ToString();
}
