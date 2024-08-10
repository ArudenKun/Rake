using Dapplo.Microsoft.Extensions.Hosting.Wpf;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Rake.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : INavigationWindow, IWpfShell
{
    public MainWindow()
    {
        InitializeComponent();
    }

    partial void BeforeInitializeComponent()
    {
        // SystemThemeWatcher.Watch(this);
    }

    partial void AfterInitializeComponent()
    {
        ViewModel.NavigationService.SetNavigationControl(RootNavigation);
        ViewModel.SnackbarService.SetSnackbarPresenter(RootSnackbarPresenter);
        ViewModel.ContentDialogService.SetDialogHost(RootDialogHost);
        SetPageService(ViewModel.PageService);
        SetServiceProvider(ViewModel.ServiceProvider);
    }

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(IPageService pageService) =>
        RootNavigation.SetPageService(pageService);

    public void SetServiceProvider(IServiceProvider serviceProvider) =>
        RootNavigation.SetServiceProvider(serviceProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();
}
