using Rake.ViewModels;
using Rake.Views.Abstractions;

namespace Rake.Views;

public sealed partial class MainWindow : SukiWindowBase<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
