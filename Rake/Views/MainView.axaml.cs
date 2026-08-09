using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Rake.ViewModels;

namespace Rake.Views;

public partial class MainView : UserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}
