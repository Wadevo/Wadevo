namespace Wadevo.Controls;

using System.Drawing.Drawing2D;
using Wadevo.Core;

public sealed class WadevoDropdownPopup : Form
{
    private const int CornerRadius = 14;

    public WadevoDropdownPopup(Control content, int width, int height)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        BackColor = WadevoTheme.Colors.Background;
        Size = new Size(width, height);

        // The border drawn in OnPaint below was previously invisible: a Dock=Fill child
        // painted over the entire form, including the pixel the border lived on, since
        // child controls always paint on top of their parent. Padding here reserves that
        // pixel so the border actually shows - the same fix used for WadevoWindowFrame.
        Padding = new Padding(2);

        content.Dock = DockStyle.Fill;
        Controls.Add(content);

        Deactivate += (_, _) => Close();
    }

    public void ShowBelow(Control anchor)
    {
        Point belowAnchor = anchor.PointToScreen(new Point(0, anchor.Height + 6));
        Screen screen = Screen.FromControl(anchor);
        Rectangle workingArea = screen.WorkingArea;

        // If opening below would push the popup past the bottom of the screen, flip it to
        // open above the anchor instead - this was the exact cause of the Emote picker
        // getting cut off when its anchor button sat low in a window.
        int y = belowAnchor.Y + Height > workingArea.Bottom
            ? Math.Max(workingArea.Top, anchor.PointToScreen(Point.Empty).Y - Height - 6)
            : belowAnchor.Y;

        int x = Math.Clamp(belowAnchor.X, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - Width));

        Location = new Point(x, y);

        Show(anchor.FindForm());
        Activate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using Pen border = new(WadevoTheme.Colors.Accent, 2);
        Rectangle rect = new(0, 0, Width - 1, Height - 1);

        using GraphicsPath path = CreateRoundedRectangle(rect, CornerRadius);

        e.Graphics.DrawPath(border, path);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using GraphicsPath path = CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius);

        Region? previousRegion = Region;
        Region = new Region(path);
        previousRegion?.Dispose();
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        GraphicsPath path = new();

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
