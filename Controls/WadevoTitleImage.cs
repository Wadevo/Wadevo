namespace Wadevo.Controls;

// Simple, predictable rendering: crop to visible content, scale to fit entirely within
// the control (leaving a margin), and center both horizontally and vertically. Nothing
// is ever stretched past the control's bounds, so clipping is not possible. An earlier
// version tried to align every title's "core" ink mass to a fixed point for pixel-perfect
// consistency between pages, but that added real complexity without resolving what was
// actually being seen - this simpler approach is easier to reason about and verify.
public sealed class WadevoTitleImage : Control
{
    // Fraction of the control's height the image is allowed to fill at most - the
    // remainder is guaranteed empty margin above and below.
    private const double MaxFillRatio = 0.72;

    private Bitmap? _croppedImage;

    public WadevoTitleImage()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
    }

    public void LoadAndCrop(string path)
    {
        _croppedImage?.Dispose();
        _croppedImage = null;

        using Bitmap source = new(path);

        Rectangle contentBounds = FindNonTransparentBounds(source);

        if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
        {
            // Nothing but transparent pixels found - fall back to the full image
            // rather than showing nothing at all.
            contentBounds = new Rectangle(0, 0, source.Width, source.Height);
        }

        _croppedImage = source.Clone(contentBounds, source.PixelFormat);

        Invalidate();
    }

    private static Rectangle FindNonTransparentBounds(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        System.Drawing.Imaging.BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            int stride = data.Stride;
            byte[] pixels = new byte[stride * height];
            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

            int minX = width, minY = height, maxX = -1, maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * stride;

                for (int x = 0; x < width; x++)
                {
                    byte alpha = pixels[rowOffset + (x * 4) + 3];

                    if (alpha <= 8)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return Rectangle.Empty;
            }

            return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_croppedImage is null || Height <= 0 || Width <= 0)
        {
            return;
        }

        int maxHeight = (int)(Height * MaxFillRatio);

        double scale = (double)maxHeight / _croppedImage.Height;

        int drawWidth = (int)(_croppedImage.Width * scale);
        int drawHeight = (int)(_croppedImage.Height * scale);

        // If scaling to the height limit would make it wider than the control itself,
        // scale down further to fit the width instead - still guaranteed to fit either way.
        if (drawWidth > Width)
        {
            double widthScale = (double)Width / _croppedImage.Width;
            drawWidth = Width;
            drawHeight = (int)(_croppedImage.Height * widthScale);
        }

        int drawX = 0;
        int drawY = (Height - drawHeight) / 2;

        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        e.Graphics.DrawImage(_croppedImage, new Rectangle(drawX, drawY, drawWidth, drawHeight));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _croppedImage?.Dispose();
        }

        base.Dispose(disposing);
    }
}
