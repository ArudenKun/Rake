using Rake.ViewModels;
using Rake.Views.Abstractions;

namespace Rake.Views;

public partial class UpdateView : UserControlBase<UpdateViewModel>
{
    public UpdateView()
    {
        InitializeComponent();
    }
}
