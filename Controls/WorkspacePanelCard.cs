namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WorkspacePanelCard : Panel
{
    private readonly Panel _titleBar = new();
    private readonly Label _titleLabel = new();
    private readonly Button _closeButton = new();
    private readonly Panel _contentHost = new();
    private readonly Label _resizeGrip = new();

    private bool _isDragging;
    private Point _dragStartOffset;

    private bool _isResizing;
    private Point _resizeStartMouse;
    private Size _resizeStartSize = new(220, 160);

    public event EventHandler? CloseRequested;

    public string PanelId { get; init; } = "";

    public Panel ContentHost => _contentHost;

    public WorkspacePanelCard(string title)
    {
        BackColor = WadevoTheme.Colors.BackgroundSoft;
        BorderStyle = BorderStyle.FixedSingle;
        MinimumSize = new Size(220, 160);

        _titleBar.Dock = DockStyle.Top;
        _titleBar.Height = 36;
        _titleBar.BackColor = WadevoTheme.Colors.Background;
        _titleBar.Cursor = Cursors.SizeAll;

        _titleLabel.Text = title;
        _titleLabel.Location = new Point(10, 8);
        _titleLabel.Size = new Size(190, 20);
        _titleLabel.Font = WadevoTheme.Fonts.Bold;
        _titleLabel.ForeColor = WadevoTheme.Colors.Accent;
        _titleLabel.BackColor = Color.Transparent;
        _titleLabel.AutoEllipsis = true;

        // A plain Button here, deliberately not WadevoButton - WadevoButton enforces a
        // hardcoded 90x36 minimum size, which badly overflowed this compact title bar
        // (the same class of bug the Commands filter row had earlier). This matches the
        // same accessible, high-contrast close-button pattern already used in
        // WadevoDialogForm and WadevoWindowFrame.
        _closeButton.Text = "×";
        _closeButton.Size = new Size(36, 36);
        _closeButton.Dock = DockStyle.Right;
        _closeButton.FlatStyle = FlatStyle.Flat;
        _closeButton.BackColor = WadevoTheme.Colors.Background;
        _closeButton.ForeColor = WadevoTheme.Colors.Text;
        _closeButton.Font = new Font(WadevoTheme.Fonts.Medium.FontFamily, 13F, FontStyle.Bold);
        _closeButton.Cursor = Cursors.Hand;
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.FlatAppearance.MouseOverBackColor = WadevoTheme.Colors.Error;
        _closeButton.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.BackColor = Color.Transparent;

        _resizeGrip.Text = "◢";
        _resizeGrip.Size = new Size(20, 20);
        _resizeGrip.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _resizeGrip.ForeColor = WadevoTheme.Colors.TextMuted;
        _resizeGrip.Font = WadevoTheme.Fonts.Small;
        _resizeGrip.BackColor = Color.Transparent;
        _resizeGrip.Cursor = Cursors.SizeNWSE;
        _resizeGrip.TextAlign = ContentAlignment.MiddleCenter;

        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_closeButton);

        Controls.Add(_contentHost);
        Controls.Add(_resizeGrip);
        Controls.Add(_titleBar);

        Resize += (_, _) => PositionResizeGrip();
        PositionResizeGrip();

        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += TitleBar_MouseUp;

        _titleLabel.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseMove += TitleBar_MouseMove;
        _titleLabel.MouseUp += TitleBar_MouseUp;

        _resizeGrip.MouseDown += ResizeGrip_MouseDown;
        _resizeGrip.MouseMove += ResizeGrip_MouseMove;
        _resizeGrip.MouseUp += ResizeGrip_MouseUp;
    }

    private void PositionResizeGrip()
    {
        _resizeGrip.Location = new Point(Width - _resizeGrip.Width - 2, Height - _resizeGrip.Height - 2);
        _resizeGrip.BringToFront();
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isDragging = true;
        _dragStartOffset = e.Location;

        if (sender is Control control && control != _titleBar)
        {
            _dragStartOffset = new Point(
                _dragStartOffset.X + control.Left,
                _dragStartOffset.Y + control.Top);
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging || Parent is null)
        {
            return;
        }

        Point cursorInParent = Parent.PointToClient(Cursor.Position);

        int newX = cursorInParent.X - _dragStartOffset.X;
        int newY = cursorInParent.Y - _dragStartOffset.Y;

        Location = new Point(Math.Max(0, newX), Math.Max(0, newY));
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void ResizeGrip_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _isResizing = true;
        _resizeStartMouse = Cursor.Position;
        _resizeStartSize = Size;
    }

    private void ResizeGrip_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        Point current = Cursor.Position;

        int deltaX = current.X - _resizeStartMouse.X;
        int deltaY = current.Y - _resizeStartMouse.Y;

        Size = new Size(
            Math.Max(MinimumSize.Width, _resizeStartSize.Width + deltaX),
            Math.Max(MinimumSize.Height, _resizeStartSize.Height + deltaY));
    }

    private void ResizeGrip_MouseUp(object? sender, MouseEventArgs e)
    {
        _isResizing = false;
    }
}
