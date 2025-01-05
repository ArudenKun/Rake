using Rake.ViewModels;
using Rake.Views.Abstractions;

namespace Rake.Views;

public sealed partial class MainWindow : SukiWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
