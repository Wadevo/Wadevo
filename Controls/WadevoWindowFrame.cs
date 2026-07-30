using Wadevo.Core;

namespace Wadevo.Controls;

public class WadevoWindowFrame : UserControl
{
    public Panel ContentHost { get; }

    private readonly Panel _titleBar;
    private readonly List<Button> _titleBarButtons = new();

    public WadevoWindowFrame()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(1);
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        _titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42,
            BackColor = WadevoTheme.Colors.Background
        };

        Label title = new()
        {
            Text = WadevoBrand.AppName,
            Dock = DockStyle.Left,
            Width = 180,
            ForeColor = WadevoTheme.Colors.Accent,
            Font = WadevoTheme.Fonts.Small,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };

        Button close = CreateWindowButton("×");
        close.Click += (_, _) => FindForm()?.Close();
        close.FlatAppearance.MouseOverBackColor = WadevoTheme.Colors.Error;

        Button max = CreateWindowButton("□");
        max.Click += (_, _) =>
        {
            Form? form = FindForm();

            if (form == null)
                return;

            form.WindowState =
                form.WindowState == FormWindowState.Maximized
                    ? FormWindowState.Normal
                    : FormWindowState.Maximized;
        };

        Button min = CreateWindowButton("—");
        min.Click += (_, _) =>
        {
            Form? form = FindForm();

            if (form != null)
                form.WindowState = FormWindowState.Minimized;
        };

        // Restored: the manual ReleaseCapture+SendMessage(WM_NCLBUTTONDOWN, HTCAPTION)
        // drag trigger. This is the same mechanism the app always used to move the
        // window - it's kept as the primary drag path since it's proven to work.
        // MainForm's CreateParams override now also grants this window real
        // WS_THICKFRAME/WS_MAXIMIZEBOX styles, so this same trick additionally
        // enables Aero Snap now (drag-to-top-edge maximizes, drag-to-side halves) -
        // Snap is a property of the OS-level drag-move loop this triggers, not of
        // how the drag was initiated.
        _titleBar.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                NativeMethods.ReleaseCaptureAndMoveWindow(FindForm());
        };

        _titleBarButtons.Add(min);
        _titleBarButtons.Add(max);
        _titleBarButtons.Add(close);

        _titleBar.Controls.Add(min);
        _titleBar.Controls.Add(max);
        _titleBar.Controls.Add(close);
        _titleBar.Controls.Add(title);

        ContentHost = new Panel()
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background
        };

        Controls.Add(ContentHost);
        Controls.Add(_titleBar);
    }

    // Called from MainForm's WM_NCHITTEST handling. Since this frame is Dock=Fill with no
    // siblings, its own coordinate space is identical to the Form's client coordinate space,
    // so the point passed in needs no translation.
    public bool IsCaptionHit(Point formClientPoint)
    {
        if (!_titleBar.Bounds.Contains(formClientPoint))
        {
            return false;
        }

        foreach (Button button in _titleBarButtons)
        {
            Rectangle buttonBoundsInFrame = new(
                _titleBar.Left + button.Left,
                _titleBar.Top + button.Top,
                button.Width,
                button.Height);

            if (buttonBoundsInFrame.Contains(formClientPoint))
            {
                return false;
            }
        }

        return true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using Pen border = new(Color.FromArgb(220, WadevoTheme.Colors.Border), 2);

        Rectangle rect = new(
            0,
            0,
            Width - 1,
            Height - 1);

        e.Graphics.DrawRectangle(border, rect);
    }

    private static Button CreateWindowButton(string text)
    {
        Button button = new()
        {
            Text = text,
            Dock = DockStyle.Right,
            Width = 56,
            FlatStyle = FlatStyle.Flat,
            BackColor = WadevoTheme.Colors.Background,
            ForeColor = WadevoTheme.Colors.Text,
            Font = new Font(WadevoTheme.Fonts.Medium.FontFamily, 14F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = WadevoTheme.Colors.BackgroundSoft;
        button.FlatAppearance.MouseDownBackColor = WadevoTheme.Colors.AccentDark;

        return button;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern nint SendMessage(
            nint hWnd,
            int msg,
            int wParam,
            int lParam);

        public static void ReleaseCaptureAndMoveWindow(Form? form)
        {
            if (form == null)
                return;

            ReleaseCapture();
            SendMessage(form.Handle, 0xA1, 0x2, 0);
        }
    }
}
