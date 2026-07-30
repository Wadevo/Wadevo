namespace Wadevo.Controls;

public static class WadevoDesignerElementControlFactory
{
    public static WadevoDesignerElementControl Create(WadevoDesignerElementState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new WadevoDesignerElementControl(state);
    }
}