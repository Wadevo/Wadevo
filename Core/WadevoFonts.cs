using System.Drawing.Text;

namespace Wadevo.Core;

public static class WadevoFonts
{
    private static readonly PrivateFontCollection Fonts = new();

    static WadevoFonts()
    {
        string fontPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "Fonts",
            "moonhouse.ttf");

        if (File.Exists(fontPath))
        {
            Fonts.AddFontFile(fontPath);
        }
    }

    public static Font Brand(float size, FontStyle style = FontStyle.Regular)
    {
        if (Fonts.Families.Length > 0)
        {
            return new Font(
                Fonts.Families[0],
                size,
                style);
        }

        return new Font(
            "Segoe UI",
            size,
            style);
    }
}