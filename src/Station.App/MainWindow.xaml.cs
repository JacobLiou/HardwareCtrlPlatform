using Station.App.ViewModels;

namespace Station.App;

public partial class MainWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
