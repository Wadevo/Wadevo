namespace Wadevo.Controls;

using System.Drawing.Drawing2D;
using Wadevo.Core;

public sealed class WadevoScrollBar : Control
{
    private int _contentHeight;
    private int _viewportHeight;
    private int _scrollValue;

    private bool _isDragging;
    private int _dragStartMouseY;
    private int _dragStartValue;

    public event EventHandler<int>? ScrollChanged;

    public int ScrollValue => _scrollValue;

    public WadevoScrollBar()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);

        Width = 10;
    }

    public void SetRange(int contentHeight, int viewportHeight)
    {
        _contentHeight = Math.Max(0, contentHeight);
        _viewportHeight = Math.Max(0, viewportHeight);

        int maxScroll = Math.Max(0, _contentHeight - _viewportHeight);
        _scrollValue = Math.Min(_scrollValue, maxScroll);

        Invalidate();
    }

    public void ScrollBy(int delta)
    {
        SetValue(_scrollValue + delta);
    }

    private void SetValue(int value)
    {
        int maxScroll = Math.Max(0, _contentHeight - _viewportHeight);
        int clamped = Math.Max(0, Math.Min(value, maxScroll));

        if (clamped == _scrollValue)
        {
            return;
        }

        _scrollValue = clamped;
        Invalidate();
        ScrollChanged?.Invoke(this, _scrollValue);
    }

    private (int Top, int Height) GetThumbBounds()
    {
        if (_contentHeight <= _viewportHeight || _contentHeight <= 0 || Height <= 0)
        {
            return (0, Height);
        }

        int trackHeight = Height;
        int thumbHeight = Math.Max(24, (int)((double)_viewportHeight / _contentHeight * trackHeight));
        thumbHeight = Math.Min(thumbHeight, trackHeight);

        int maxScroll = _contentHeight - _viewportHeight;
        int maxThumbTop = trackHeight - thumbHeight;

        int thumbTop = maxScroll <= 0 ? 0 : (int)((double)_scrollValue / maxScroll * maxThumbTop);

        return (thumbTop, thumbHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Color clearColor = Parent?.BackColor == Color.Transparent
            ? WadevoTheme.Colors.Background
            : Parent?.BackColor ?? WadevoTheme.Colors.Background;

        e.Graphics.Clear(clearColor);

        if (_contentHeight <= _viewportHeight || Width <= 0 || Height <= 0)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle trackRect = new(2, 0, Math.Max(1, Width - 4), Height);

        using GraphicsPath trackPath = RoundedRect(trackRect, 4);
        using SolidBrush trackBrush = new(Color.FromArgb(30, 255, 255, 255));
        e.Graphics.FillPath(trackBrush, trackPath);

        (int thumbTop, int thumbHeight) = GetThumbBounds();

        if (thumbHeight <= 0)
        {
            return;
        }

        Rectangle thumbRect = new(1, thumbTop, Math.Max(1, Width - 2), thumbHeight);

        using GraphicsPath thumbPath = RoundedRect(thumbRect, 4);
        using SolidBrush thumbBrush = new(WadevoTheme.Colors.Accent);
        e.Graphics.FillPath(thumbBrush, thumbPath);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        (int thumbTop, int thumbHeight) = GetThumbBounds();

        if (e.Y >= thumbTop && e.Y <= thumbTop + thumbHeight)
        {
            _isDragging = true;
            _dragStartMouseY = e.Y;
            _dragStartValue = _scrollValue;
            Capture = true;
        }
        else
        {
            int direction = e.Y < thumbTop ? -1 : 1;
            ScrollBy(direction * Math.Max(1, _viewportHeight));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isDragging)
        {
            return;
        }

        (_, int thumbHeight) = GetThumbBounds();
        int maxThumbTop = Math.Max(1, Height - thumbHeight);
        int maxScroll = Math.Max(0, _contentHeight - _viewportHeight);

        int deltaPixels = e.Y - _dragStartMouseY;
        int deltaValue = (int)((double)deltaPixels / maxThumbTop * maxScroll);

        SetValue(_dragStartValue + deltaValue);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        _isDragging = false;
        Capture = false;
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));

        if (diameter <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
