using Station.App.ViewModels;

namespace Station.App;

public partial class MainWindow
{
    public MainWindow(MainShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell;
    }
}
