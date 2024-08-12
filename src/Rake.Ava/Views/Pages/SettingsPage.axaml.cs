using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveMarbles.ObservableEvents;

namespace Rake.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();

        NativeWebView.Initialized += NativeWebViewOnInitialized;
    }

    private void NativeWebViewOnInitialized(object? sender, EventArgs e)
    {
        NativeWebView.Source = new Uri("https://www.google.com");
    }
}
