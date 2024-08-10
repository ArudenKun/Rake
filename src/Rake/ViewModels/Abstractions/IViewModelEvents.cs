namespace Rake.ViewModels.Abstractions;

public interface IViewModelEvents
{
    void Loaded();
    void Unloaded();
    void AttachedToVisualTree();
    void DetachedFromVisualTree();
}
