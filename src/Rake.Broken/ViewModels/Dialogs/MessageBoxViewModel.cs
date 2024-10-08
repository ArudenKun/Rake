﻿using CommunityToolkit.Mvvm.ComponentModel;
using Rake.ViewModels.Common;

namespace Rake.ViewModels.Dialogs;

public sealed partial class MessageBoxViewModel : BaseDialogViewModel
{
    [ObservableProperty]
    private string _title = "Title";

    [ObservableProperty]
    private string _message = "Message";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDefaultButtonVisible))]
    [NotifyPropertyChangedFor(nameof(ButtonsCount))]
    private string _defaultButtonText = "OK";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCancelButtonVisible))]
    [NotifyPropertyChangedFor(nameof(ButtonsCount))]
    private string _cancelButtonText = "Cancel";

    public bool IsDefaultButtonVisible => !string.IsNullOrWhiteSpace(DefaultButtonText);

    public bool IsCancelButtonVisible => !string.IsNullOrWhiteSpace(CancelButtonText);

    public int ButtonsCount => (IsDefaultButtonVisible ? 1 : 0) + (IsCancelButtonVisible ? 1 : 0);
}
