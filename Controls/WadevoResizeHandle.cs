namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public enum WadevoResizeDirection
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Right,
    BottomLeft,
    Bottom,
    BottomRight
}

public sealed class WadevoResizeHandle : Control
{
    private bool _isDragging;
    private Point _dragStartMouse;
    private Rectangle _dragStartBounds;

    public event EventHandler<ResizeHandleChangedEventArgs>? ResizeChanged;

    public WadevoResizeHandle()
    {
        Size = new Size(16, 16);
        Cursor = Cursors.SizeNWSE;
        BackColor = WadevoTheme.Colors.Card;
        DoubleBuffered = true;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? TargetControl { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public WadevoResizeDirection Direction { get; set; } = WadevoResizeDirection.BottomRight;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left || TargetControl is null)
        {
            return;
        }

        _isDragging = true;
        _dragStartMouse = Cursor.Position;
        _dragStartBounds = TargetControl.Bounds;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isDragging || TargetControl is null)
        {
            return;
        }

        Point currentMouse = Cursor.Position;

        int deltaX = currentMouse.X - _dragStartMouse.X;
        int deltaY = currentMouse.Y - _dragStartMouse.Y;

        Rectangle newBounds = CalculateBounds(deltaX, deltaY);

        TargetControl.Bounds = newBounds;

        ResizeChanged?.Invoke(
            this,
            new ResizeHandleChangedEventArgs(TargetControl, TargetControl.Size));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        _isDragging = false;
        Capture = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using SolidBrush fillBrush = new(WadevoTheme.Colors.Accent);
        using Pen borderPen = new(WadevoTheme.Colors.Background, 2);

        Rectangle handleRect = new(2, 2, Width - 4, Height - 4);

        e.Graphics.FillEllipse(fillBrush, handleRect);
        e.Graphics.DrawEllipse(borderPen, handleRect);
    }

    private Rectangle CalculateBounds(int deltaX, int deltaY)
    {
        const int minimumWidth = 80;
        const int minimumHeight = 60;

        int x = _dragStartBounds.X;
        int y = _dragStartBounds.Y;
        int width = _dragStartBounds.Width;
        int height = _dragStartBounds.Height;

        switch (Direction)
        {
            case WadevoResizeDirection.TopLeft:
                x += deltaX;
                y += deltaY;
                width -= deltaX;
                height -= deltaY;
                break;

            case WadevoResizeDirection.Top:
                y += deltaY;
                height -= deltaY;
                break;

            case WadevoResizeDirection.TopRight:
                y += deltaY;
                width += deltaX;
                height -= deltaY;
                break;

            case WadevoResizeDirection.Left:
                x += deltaX;
                width -= deltaX;
                break;

            case WadevoResizeDirection.Right:
                width += deltaX;
                break;

            case WadevoResizeDirection.BottomLeft:
                x += deltaX;
                width -= deltaX;
                height += deltaY;
                break;

            case WadevoResizeDirection.Bottom:
                height += deltaY;
                break;

            case WadevoResizeDirection.BottomRight:
                width += deltaX;
                height += deltaY;
                break;
        }

        if (width < minimumWidth)
        {
            if (Direction is WadevoResizeDirection.TopLeft or WadevoResizeDirection.Left or WadevoResizeDirection.BottomLeft)
            {
                x = _dragStartBounds.Right - minimumWidth;
            }

            width = minimumWidth;
        }

        if (height < minimumHeight)
        {
            if (Direction is WadevoResizeDirection.TopLeft or WadevoResizeDirection.Top or WadevoResizeDirection.TopRight)
            {
                y = _dragStartBounds.Bottom - minimumHeight;
            }

            height = minimumHeight;
        }

        x = Math.Max(0, x);
        y = Math.Max(0, y);

        return new Rectangle(x, y, width, height);
    }
}

public sealed class ResizeHandleChangedEventArgs : EventArgs
{
    public ResizeHandleChangedEventArgs(Control targetControl, Size newSize)
    {
        TargetControl = targetControl;
        NewSize = newSize;
    }

    public Control TargetControl { get; }

    public Size NewSize { get; }
}