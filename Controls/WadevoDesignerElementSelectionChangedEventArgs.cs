namespace Wadevo.Controls;

public sealed class WadevoDesignerElementSelectionChangedEventArgs : EventArgs
{
    public WadevoDesignerElementSelectionChangedEventArgs(IReadOnlyList<WadevoDesignerElementState> selectedElements)
    {
        SelectedElements = selectedElements;
    }

    public IReadOnlyList<WadevoDesignerElementState> SelectedElements { get; }

    public bool HasSelection => SelectedElements.Count > 0;

    public WadevoDesignerElementState? PrimarySelection => HasSelection
        ? SelectedElements[0]
        : null;
}