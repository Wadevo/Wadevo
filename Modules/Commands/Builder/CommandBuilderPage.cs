namespace Wadevo.Modules.Commands.Builder;

public abstract class CommandBuilderPage : UserControl
{
    public abstract string PageTitle { get; }
    public abstract string PageSubtitle { get; }

    public virtual void LoadFromState(BuilderState state) { }
    public virtual void SaveToState(BuilderState state) { }
    public virtual bool CanMoveNext() => true;

    // Called explicitly by the wizard right after it sets this page's size - deliberately
    // not relying on the Resize event, since Dock=Fill can size a page correctly before the
    // wizard's own explicit .Size assignment runs, meaning that assignment sets the same
    // value and Resize never actually fires.
    public virtual void OnHostResized() { }
}