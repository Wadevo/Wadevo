namespace Wadevo.Controls;

using Wadevo.Core;

public static class WadevoDesignerColors
{
    public static Color CanvasBackground => WadevoTheme.Colors.Background;

    public static Color SurfaceBackground => WadevoTheme.Colors.Card;

    public static Color Selection => WadevoTheme.Colors.Cyan;

    public static Color SelectionGlow => Color.FromArgb(90, WadevoTheme.Colors.Cyan);

    public static Color GridMinor => Color.FromArgb(24, Color.White);

    public static Color GridMajor => Color.FromArgb(42, Color.White);

    public static Color RulerBackground => Color.FromArgb(34, Color.White);

    public static Color RulerCorner => Color.FromArgb(55, Color.White);

    public static Color RulerBorder => Color.FromArgb(70, Color.White);

    public static Color RulerTick => Color.FromArgb(95, Color.White);

    public static Color Guide => Color.FromArgb(210, WadevoTheme.Colors.Cyan);

    public static Color MarqueeFill => Color.FromArgb(28, WadevoTheme.Colors.Cyan);

    public static Color MarqueeBorder => Color.FromArgb(180, WadevoTheme.Colors.Cyan);

    public static Color ElementFill => Color.FromArgb(32, Color.White);

    public static Color ElementBorder => Color.FromArgb(60, Color.White);
}