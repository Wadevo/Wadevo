namespace Wadevo.Services;

// The single source of truth for "given a canvas, a background image, and a scale mode
// (Fill/Fit/Stretch/Center) plus width%/height%/offset adjustments, what exact pixel
// rectangle should the image be drawn at?" This used to be implemented separately in
// WadevoDesignerCanvas.cs (the live Designer preview) and OverlayServer.cs (both the
// regular overlay export and the Alert popup), which had already drifted out of sync once
// before. All three now call this instead of keeping their own copy.
public static class BackgroundRectCalculator
{
    public static (int X, int Y, int Width, int Height) ComputeDestRect(
        int canvasX, int canvasY, int canvasWidth, int canvasHeight,
        int imageWidth, int imageHeight,
        string scaleMode, int widthPercent, int heightPercent, int offsetX, int offsetY)
    {
        (int baseX, int baseY, int baseWidth, int baseHeight) = ComputeBaseDestRect(
            canvasX, canvasY, canvasWidth, canvasHeight, imageWidth, imageHeight, scaleMode);

        int scaledX;
        int scaledY;
        int scaledWidth;
        int scaledHeight;

        if (widthPercent == 100 && heightPercent == 100)
        {
            scaledX = baseX;
            scaledY = baseY;
            scaledWidth = baseWidth;
            scaledHeight = baseHeight;
        }
        else
        {
            double widthFactor = Math.Max(0.1, widthPercent / 100.0);
            double heightFactor = Math.Max(0.1, heightPercent / 100.0);

            scaledWidth = (int)(baseWidth * widthFactor);
            scaledHeight = (int)(baseHeight * heightFactor);

            int centerX = baseX + baseWidth / 2;
            int centerY = baseY + baseHeight / 2;

            scaledX = centerX - scaledWidth / 2;
            scaledY = centerY - scaledHeight / 2;
        }

        // Click-and-drag panning offset, applied last so it shifts the already-scaled
        // image regardless of which scale mode or size is active.
        return (scaledX + offsetX, scaledY + offsetY, scaledWidth, scaledHeight);
    }

    private static (int X, int Y, int Width, int Height) ComputeBaseDestRect(
        int canvasX, int canvasY, int canvasWidth, int canvasHeight,
        int imageWidth, int imageHeight, string scaleMode)
    {
        if (scaleMode == "Stretch")
        {
            return (canvasX, canvasY, canvasWidth, canvasHeight);
        }

        if (scaleMode == "Center")
        {
            int centerX = canvasX + (canvasWidth - imageWidth) / 2;
            int centerY = canvasY + (canvasHeight - imageHeight) / 2;

            return (centerX, centerY, imageWidth, imageHeight);
        }

        double imageAspect = (double)imageWidth / imageHeight;
        double canvasAspect = (double)canvasWidth / canvasHeight;

        // Fit: entire image visible, may letterbox. Fill: canvas fully covered, image may
        // crop at the edges.
        bool scaleToWidth = scaleMode == "Fit"
            ? imageAspect > canvasAspect
            : imageAspect < canvasAspect;

        int width;
        int height;

        if (scaleToWidth)
        {
            width = canvasWidth;
            height = (int)(canvasWidth / imageAspect);
        }
        else
        {
            height = canvasHeight;
            width = (int)(canvasHeight * imageAspect);
        }

        int x = canvasX + (canvasWidth - width) / 2;
        int y = canvasY + (canvasHeight - height) / 2;

        return (x, y, width, height);
    }
}
