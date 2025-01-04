using Rake.ViewModels;
using Rake.Views.Abstractions;

namespace Rake.Views;

public sealed partial class MainWindow : Window<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
