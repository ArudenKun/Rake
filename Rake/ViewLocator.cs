using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;
using Rake.ViewModels;
using Rake.Views;
using ServiceScan.SourceGenerator;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace Rake;

[ExposeServices(typeof(ViewLocator))]
public partial class ViewLocator : IDataTemplate, ISingletonDependency
{
    private static readonly Dictionary<Type, Func<Control>> ViewMap = new();

    public ViewLocator(IServiceProvider serviceProvider) => Register(serviceProvider);

    [ScanForTypes(AssignableTo = typeof(IView<>), Handler = nameof(RegisterHandler))]
    private partial void Register(IServiceProvider serviceProvider);

    private static void RegisterHandler<TView, TViewModel>(IServiceProvider serviceProvider)
        where TView : Control
        where TViewModel : ViewModel =>
        _ = ViewMap.TryAdd(typeof(TViewModel), serviceProvider.GetRequiredService<TView>);

    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        var viewModelType = ProxyHelper.GetUnProxiedType(param);
        var factory = ViewMap.GetValueOrDefault(viewModelType);
        if (factory is null)
            return new TextBlock { Text = "No view found for " + viewModelType.FullName };
        var view = factory();
        view.DataContext = param;
        return view;
    }

    public bool Match(object? data) => data is ViewModel;
}
