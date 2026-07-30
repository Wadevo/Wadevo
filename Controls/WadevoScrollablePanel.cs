namespace Wadevo.Controls;

public sealed class WadevoScrollablePanel : Panel
{
    private const int WheelStep = 60;

    private readonly WadevoDoubleBufferedPanel _viewport = new();
    private readonly DoubleBufferedFlowLayoutPanel _content = new();
    private readonly WadevoScrollBar _scrollBar = new();

    public FlowLayoutPanel Content => _content;

    public WadevoScrollablePanel()
    {
        BackColor = Color.Transparent;
        TabStop = true;
        DoubleBuffered = true;
        ResizeRedraw = true;

        _viewport.Dock = DockStyle.Fill;
        _viewport.BackColor = Color.Transparent;

        _content.Location = new Point(0, 0);
        _content.FlowDirection = FlowDirection.TopDown;
        _content.WrapContents = false;
        _content.BackColor = Color.Transparent;
        _content.AutoScroll = false;

        _viewport.Controls.Add(_content);

        _scrollBar.Dock = DockStyle.Right;
        _scrollBar.ScrollChanged += (_, value) => ApplyScroll(value);

        Controls.Add(_viewport);
        Controls.Add(_scrollBar);

        _content.SizeChanged += (_, _) => UpdateScrollBar();
        Resize += (_, _) => UpdateScrollBar();

        // A plain Panel doesn't take focus by default, and Windows sends mouse-wheel
        // messages to whichever control currently has focus - not whatever the mouse
        // happens to be over. Focusing on hover is what makes "scroll with the wheel"
        // work without first clicking into the list.
        MouseEnter += (_, _) => Focus();
        _viewport.MouseEnter += (_, _) => Focus();
        _content.MouseEnter += (_, _) => Focus();

        UpdateScrollBar();
    }

    public void RefreshLayout()
    {
        UpdateScrollBar();
    }

    private void UpdateScrollBar()
    {
        int viewportWidth = Math.Max(0, _viewport.ClientSize.Width);
        int viewportHeight = _viewport.ClientSize.Height;

        int preferredHeight = _content.GetPreferredSize(new Size(viewportWidth, 0)).Height;

        _content.Width = viewportWidth;
        _content.Height = Math.Max(preferredHeight, viewportHeight);

        _scrollBar.SetRange(preferredHeight, viewportHeight);
        _scrollBar.Visible = preferredHeight > viewportHeight;

        ApplyScroll(_scrollBar.ScrollValue);
    }

    private void ApplyScroll(int value)
    {
        _content.Top = -value;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        int delta = e.Delta > 0 ? -WheelStep : WheelStep;
        _scrollBar.ScrollBy(delta);
    }

    // DoubleBuffered is protected on Control, so it can't be set directly from outside -
    // this small subclass just enables it internally. Needed because this is the actual
    // container items get added to and removed from during a list refresh; without this,
    // each individual add/remove could trigger its own visible repaint.
    private sealed class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            DoubleBuffered = true;
        }
    }
}
