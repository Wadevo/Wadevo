namespace Wadevo.Controls;

public sealed class WadevoDesignerElementEventArgs : EventArgs
{
    public WadevoDesignerElementEventArgs(WadevoDesignerElementState element)
    {
        Element = element;
    }

    public WadevoDesignerElementState Element { get; }
}