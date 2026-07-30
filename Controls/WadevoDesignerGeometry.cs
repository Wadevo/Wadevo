namespace Wadevo.Controls;

public static class WadevoDesignerGeometry
{
    public static Rectangle CreateRectangleFromPoints(Point start, Point end)
    {
        return new Rectangle(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(start.X - end.X),
            Math.Abs(start.Y - end.Y));
    }

    public static Point Snap(Point point, int gridSize)
    {
        return new Point(
            Snap(point.X, gridSize),
            Snap(point.Y, gridSize));
    }

    public static Size Snap(Size size, int gridSize)
    {
        return new Size(
            Math.Max(gridSize, Snap(size.Width, gridSize)),
            Math.Max(gridSize, Snap(size.Height, gridSize)));
    }

    public static Rectangle Snap(Rectangle rectangle, int gridSize)
    {
        return new Rectangle(
            Snap(rectangle.Location, gridSize),
            Snap(rectangle.Size, gridSize));
    }

    public static int Snap(int value, int gridSize)
    {
        if (gridSize <= 0)
        {
            return value;
        }

        return (int)Math.Round(value / (double)gridSize) * gridSize;
    }

    public static Rectangle KeepInside(Rectangle bounds, Rectangle container)
    {
        int x = Math.Clamp(bounds.X, container.Left, Math.Max(container.Left, container.Right - bounds.Width));
        int y = Math.Clamp(bounds.Y, container.Top, Math.Max(container.Top, container.Bottom - bounds.Height));

        return new Rectangle(x, y, bounds.Width, bounds.Height);
    }
}