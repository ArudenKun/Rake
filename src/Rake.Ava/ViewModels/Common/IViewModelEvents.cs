namespace Rake.ViewModels.Common;

public interface IViewModelEvents
{
    void Loaded();
    void Unloaded();
    void AttachedToVisualTree();
    void DetachedFromVisualTree();
}
