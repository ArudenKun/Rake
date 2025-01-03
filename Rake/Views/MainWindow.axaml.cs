using Rake.ViewModels;
using Rake.Views.Abstractions;

namespace Rake.Views;

public partial class MainWindow : SukiWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
