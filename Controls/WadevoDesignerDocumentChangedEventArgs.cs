namespace Wadevo.Controls;

public sealed class WadevoDesignerDocumentChangedEventArgs : EventArgs
{
    public WadevoDesignerDocumentChangedEventArgs(
        WadevoDesignerDocument document,
        WadevoDesignerElementState? changedElement = null)
    {
        Document = document;
        ChangedElement = changedElement;
    }

    public WadevoDesignerDocument Document { get; }

    public WadevoDesignerElementState? ChangedElement { get; }

    public bool HasChangedElement => ChangedElement is not null;
}