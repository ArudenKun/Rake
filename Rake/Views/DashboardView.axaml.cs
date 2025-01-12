using Rake.ViewModels;
using Rake.Views.Abstractions;

namespace Rake.Views;

public partial class DashboardView : UserControlBase<DashboardViewModel>
{
    public DashboardView()
    {
        InitializeComponent();
    }
}
