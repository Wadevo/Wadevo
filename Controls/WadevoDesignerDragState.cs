namespace Wadevo.Controls;

public sealed class WadevoDesignerDragState
{
    private readonly Dictionary<string, Rectangle> _startBoundsByElementId = [];

    public bool IsDragging { get; private set; }

    public Point StartMouse { get; private set; }

    public IReadOnlyDictionary<string, Rectangle> StartBoundsByElementId => _startBoundsByElementId;

    public void Begin(Point mouseLocation, IEnumerable<WadevoDesignerElementState> elements)
    {
        StartMouse = mouseLocation;
        IsDragging = true;

        _startBoundsByElementId.Clear();

        foreach (WadevoDesignerElementState element in elements)
        {
            _startBoundsByElementId[element.Id] = element.Bounds;
        }
    }

    public Rectangle GetMovedBounds(WadevoDesignerElementState element, Point currentMouse, double scaleX = 1.0, double scaleY = 1.0)
    {
        if (!_startBoundsByElementId.TryGetValue(element.Id, out Rectangle startBounds))
        {
            return element.Bounds;
        }

        int deltaX = (int)((currentMouse.X - StartMouse.X) / scaleX);
        int deltaY = (int)((currentMouse.Y - StartMouse.Y) / scaleY);

        return new Rectangle(
            startBounds.X + deltaX,
            startBounds.Y + deltaY,
            startBounds.Width,
            startBounds.Height);
    }

    public void End()
    {
        IsDragging = false;
        _startBoundsByElementId.Clear();
    }
}