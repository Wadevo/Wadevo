namespace Wadevo.Controls;

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Wadevo.Core;

public abstract class WadevoDialogForm : Form
{
    private const int CornerRadius = 14;

    protected Panel ContentPanel { get; } = new();

    protected WadevoDialogForm(string title)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = WadevoTheme.Colors.Background;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        DoubleBuffered = true;

        // Reserves room for the border drawn in OnPaint below. Without this, the
        // Dock=Top titleBar and Dock=Fill ContentPanel together paint over the entire
        // client area - including the pixel the border lived on - since child controls
        // always paint on top of their parent, making the border invisible against a
        // dark background regardless of its color.
        Padding = new Padding(2);

        Panel titleBar = new()
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label titleLabel = new()
        {
            Text = title,
            Dock = DockStyle.Left,
            Width = 320,
            ForeColor = WadevoTheme.Colors.Accent,
            Font = WadevoTheme.Fonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            BackColor = Color.Transparent
        };

        Button closeButton = new()
        {
            Text = "×",
            Dock = DockStyle.Right,
            Width = 56,
            FlatStyle = FlatStyle.Flat,
            BackColor = WadevoTheme.Colors.BackgroundSoft,
            ForeColor = WadevoTheme.Colors.Text,
            Font = new Font(WadevoTheme.Fonts.Medium.FontFamily, 14F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseOverBackColor = WadevoTheme.Colors.Error;
        closeButton.FlatAppearance.MouseDownBackColor = WadevoTheme.Colors.AccentDark;

        closeButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        void StartDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCaptureAndMoveWindow(this);
            }
        }

        titleBar.MouseDown += StartDrag;
        titleLabel.MouseDown += StartDrag;

        titleBar.Controls.Add(closeButton);
        titleBar.Controls.Add(titleLabel);

        ContentPanel.Dock = DockStyle.Fill;
        ContentPanel.BackColor = WadevoTheme.Colors.Background;

        Controls.Add(ContentPanel);
        Controls.Add(titleBar);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using Pen border = new(Color.FromArgb(220, WadevoTheme.Colors.Border), 2);
        Rectangle rect = new(0, 0, Width - 1, Height - 1);

        using GraphicsPath path = CreateRoundedRectangle(rect, CornerRadius);

        e.Graphics.DrawPath(border, path);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    // Region-based window shaping actually clips the window to a rounded outline (rather
    // than just painting a rounded-looking border over a still-square window), which is
    // what makes the corners themselves genuinely rounded instead of just the border line.
    // The one accepted trade-off with this technique: GDI regions don't anti-alias, so the
    // very corner pixels are a touch harder-edged than the (anti-aliased) border line drawn
    // inside them - a common, minor compromise for WinForms rounded windows.
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

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern nint SendMessage(nint hWnd, int msg, int wParam, int lParam);

        public static void ReleaseCaptureAndMoveWindow(Form form)
        {
            ReleaseCapture();
            SendMessage(form.Handle, 0xA1, 0x2, 0);
        }
    }
}
