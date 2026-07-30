namespace Wadevo.Controls;

public sealed class WadevoDesignerCanvasState
{
    public int PreviewX { get; set; } = 90;

    public int PreviewY { get; set; } = 90;

    public int PreviewWidth { get; set; } = 420;

    public int PreviewHeight { get; set; } = 120;

    public string PreviewTitle { get; set; } = "Song ID";

    public string PreviewArtist { get; set; } = "Artist Name";

    public string PreviewSong { get; set; } = "Song Title";

    public Rectangle ToRectangle()
    {
        return new Rectangle(PreviewX, PreviewY, PreviewWidth, PreviewHeight);
    }

    public static WadevoDesignerCanvasState FromCanvas(WadevoDesignerCanvas canvas)
    {
        Rectangle bounds = canvas.PreviewBounds;

        return new WadevoDesignerCanvasState
        {
            PreviewX = bounds.X,
            PreviewY = bounds.Y,
            PreviewWidth = bounds.Width,
            PreviewHeight = bounds.Height,
            PreviewTitle = canvas.PreviewTitle,
            PreviewArtist = canvas.PreviewArtist,
            PreviewSong = canvas.PreviewSong
        };
    }
}