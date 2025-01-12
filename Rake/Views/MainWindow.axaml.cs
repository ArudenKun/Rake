using Rake.ViewModels;

namespace Rake.Views;

public sealed partial class MainWindow : AbstractSukiWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
