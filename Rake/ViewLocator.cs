using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.DependencyInjection;
using Rake.ViewModels;
using ServiceScan.SourceGenerator;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace Rake;

[ExposeServices(typeof(ViewLocator))]
public partial class ViewLocator : IDataTemplate, ISingletonDependency
{
    private static Dictionary<Type, Type> _viewToViewModel = new();
    private static Dictionary<Type, Type> _viewModelToView = new();

    private static readonly Lock Lock = new();
    private static bool _initialized;

    public ViewLocator(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public Control? Build(object? param)
    {
        if (param is null)
            return null;
        var viewModelType = ProxyHelper.GetUnProxiedType(param);
        var viewType = TryGetViewType(viewModelType);
        if (viewType is null)
            return new TextBlock { Text = "No view found for " + viewModelType.FullName };
        var view = (Control)ServiceProvider.GetRequiredService(viewType);
        view.DataContext = param;
        return view;
    }

    public bool Match(object? data)
    {
        return data is ViewModel;
    }

    [ScanForTypes(AssignableTo = typeof(Control), TypeNameFilter = "*View,*Window")]
    private static partial Type[] GetViewTypes();

    [ScanForTypes(AssignableTo = typeof(ViewModel), TypeNameFilter = "*ViewModel")]
    private static partial Type[] GetViewModelTypes();

    /// <summary>
    /// Finds the corresponding View Type for a given ViewModel Type.
    /// </summary>
    public static Type GetViewType(Type viewModelType)
    {
        EnsureInitialized();
        return _viewModelToView.TryGetValue(viewModelType, out var viewType)
            ? viewType
            : throw new KeyNotFoundException($"No View found for {viewModelType.Name}");
    }

    /// <summary>
    /// Finds the corresponding View Type for a given ViewModel Type.
    /// </summary>
    public static Type? TryGetViewType(Type viewModelType)
    {
        EnsureInitialized();
        return _viewModelToView.GetValueOrDefault(viewModelType);
    }

    /// <summary>
    /// Finds the corresponding ViewModel Type for a given View Type.
    /// </summary>
    public static Type GetViewModelType(Type viewType)
    {
        EnsureInitialized();
        return _viewToViewModel.TryGetValue(viewType, out var viewModelType)
            ? viewModelType
            : throw new KeyNotFoundException($"No ViewModel found for {viewType.Name}");
    }

    /// <summary>
    /// Finds the corresponding ViewModel Type for a given View Type.
    /// </summary>
    public static Type? TryGetViewModelType(Type viewType)
    {
        EnsureInitialized();
        return _viewToViewModel.GetValueOrDefault(viewType);
    }

    public static IReadOnlyDictionary<Type, Type> GetViewToViewModelMapping()
    {
        EnsureInitialized();
        return _viewToViewModel;
    }

    public static IReadOnlyDictionary<Type, Type> GetViewModelToViewMapping()
    {
        EnsureInitialized();
        return _viewModelToView;
    }

    private static void EnsureInitialized()
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (_initialized)
            return;

        lock (Lock)
        {
            if (_initialized)
                return;

            var viewTypes = GetViewTypes();
            var viewModelTypes = GetViewModelTypes();

            // Index ViewModels by their full Type Name (e.g., "MainWindowViewModel")
            var viewModelLookup = new Dictionary<string, Type>(
                viewModelTypes.Length,
                StringComparer.OrdinalIgnoreCase
            );

            foreach (var vmType in viewModelTypes)
            {
                viewModelLookup[vmType.Name] = vmType;
            }

            var viewToViewModelMap = new Dictionary<Type, Type>(viewTypes.Length);
            var viewModelToViewMap = new Dictionary<Type, Type>(viewModelTypes.Length);

            // Pair Views by looking up exact name + "ViewModel" (e.g., "MainWindow" -> "MainWindowViewModel")
            foreach (var viewType in viewTypes)
            {
                var expectedViewModelName = $"{viewType.Name}ViewModel";

                if (!viewModelLookup.TryGetValue(expectedViewModelName, out var viewModelType))
                    continue;

                viewToViewModelMap[viewType] = viewModelType;
                viewModelToViewMap[viewModelType] = viewType;
            }

            _viewToViewModel = viewToViewModelMap;
            _viewModelToView = viewModelToViewMap;
            _initialized = true;
        }
    }
}
