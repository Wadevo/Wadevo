using System.Drawing;

namespace Wadevo.Core;

public static class WadevoTheme
{
    public static class Colors
    {
        public static readonly Color Background = Color.FromArgb(4, 8, 6);
        public static readonly Color BackgroundSoft = Color.FromArgb(7, 14, 11);

        public static readonly Color Sidebar = Color.Black;
        public static readonly Color Panel = Color.FromArgb(9, 18, 14);
        public static readonly Color Card = Color.FromArgb(12, 24, 19);
        public static readonly Color CardHover = Color.FromArgb(18, 38, 29);

        // Official Wadevo logo green
        public static readonly Color Accent = Color.FromArgb(90, 176, 57);
        public static readonly Color AccentDark = Color.FromArgb(45, 115, 35);
        public static readonly Color AccentSoft = Color.FromArgb(18, 45, 15);

        public static readonly Color Cyan = Accent;
        public static readonly Color Purple = Accent;
        public static readonly Color Pink = Accent;
        public static readonly Color Orange = Accent;

        public static readonly Color Text = Color.FromArgb(245, 255, 250);
        public static readonly Color TextSecondary = Color.FromArgb(190, 220, 205);
        public static readonly Color TextMuted = Color.FromArgb(115, 150, 135);

        public static readonly Color Success = Accent;
        public static readonly Color Warning = Color.FromArgb(225, 255, 110);
        public static readonly Color Error = Color.FromArgb(255, 88, 108);

        public static readonly Color Border = Color.FromArgb(55, 120, 45);
        public static readonly Color BorderGlow = Accent;
    }

    public static class Fonts
    {
        public static readonly Font Small = new("Segoe UI", 8F, FontStyle.Regular);
        public static readonly Font Default = new("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font Normal = Default;
        public static readonly Font Medium = new("Segoe UI", 12F, FontStyle.Regular);
        public static readonly Font Bold = new("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font CardHeader = new("Segoe UI", 11F, FontStyle.Bold);
        public static readonly Font Header = new("Segoe UI", 20F, FontStyle.Bold);
        public static readonly Font Title = new("Segoe UI", 34F, FontStyle.Bold);

        public static readonly Font Hero =
            WadevoFonts.Brand(38F, FontStyle.Bold);

        public static readonly Font Brand =
            WadevoFonts.Brand(30F, FontStyle.Bold);
    }

    public static class Sizes
    {
        public const int BorderRadius = 24;

        public const int PaddingSmall = 8;
        public const int PaddingMedium = 18;
        public const int PaddingLarge = 28;

        // A consistent spacing rhythm (multiples of 8) for new layout work,
        // so gaps between elements stop being picked ad hoc per page.
        public const int SpaceXS = 8;
        public const int SpaceS = 16;
        public const int SpaceM = 24;
        public const int SpaceL = 32;
        public const int SpaceXL = 40;

        public const int ControlHeight = 42;
        public const int SidebarWidth = 245;
    }
}