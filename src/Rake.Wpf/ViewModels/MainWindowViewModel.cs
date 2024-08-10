using System.Collections.ObjectModel;
using Rake.ViewModels.Abstractions;
using Rake.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Rake.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    public INavigationService NavigationService { get; }
    public IPageService PageService { get; }
    public IServiceProvider ServiceProvider { get; }
    public ISnackbarService SnackbarService { get; }
    public IContentDialogService ContentDialogService { get; }

    public ObservableCollection<NavigationViewItem> Menus { get; } = [];
    public ObservableCollection<NavigationViewItem> Footers { get; } = [];

    public MainWindowViewModel(
        INavigationService navigationService,
        IPageService pageService,
        IServiceProvider serviceProvider,
        IEnumerable<BasePageViewModel> pageViewModels,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        NavigationService = navigationService;
        PageService = pageService;
        ServiceProvider = serviceProvider;
        SnackbarService = snackbarService;
        ContentDialogService = contentDialogService;

        foreach (var pageViewModel in pageViewModels.OrderBy(x => x.Index))
        {
            var nvi = new NavigationViewItem(
                pageViewModel.Name,
                pageViewModel.Icon,
                pageViewModel.ViewType
            );

            if (!pageViewModel.IsFooter)
            {
                Menus.Add(nvi);
                continue;
            }

            Footers.Add(nvi);
        }
    }

    protected override void Loaded()
    {
        NavigationService.Navigate(Menus[0].TargetPageType!);
    }

    public string Greetings => nameof(Greetings);
}
