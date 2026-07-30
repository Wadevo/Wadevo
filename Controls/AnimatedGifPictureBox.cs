namespace Wadevo.Controls;

public sealed class AnimatedGifPictureBox : PictureBox
{
    private readonly EventHandler _frameChangedHandler;
    private Image? _ownedImage;

    public AnimatedGifPictureBox()
    {
        SizeMode = PictureBoxSizeMode.Zoom;
        _frameChangedHandler = OnFrameChanged;
    }

    public void SetImage(Image image)
    {
        StopAnimation();

        _ownedImage = image;
        Image = image;

        if (ImageAnimator.CanAnimate(image))
        {
            ImageAnimator.Animate(image, _frameChangedHandler);
        }
    }

    public void ClearImage()
    {
        StopAnimation();
        Image = null;
    }

    private void OnFrameChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(Invalidate);
            }
            catch
            {
            }

            return;
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pe)
    {
        if (Image != null && ImageAnimator.CanAnimate(Image))
        {
            ImageAnimator.UpdateFrames(Image);
        }

        base.OnPaint(pe);
    }

    private void StopAnimation()
    {
        if (_ownedImage != null && ImageAnimator.CanAnimate(_ownedImage))
        {
            ImageAnimator.StopAnimate(_ownedImage, _frameChangedHandler);
        }

        _ownedImage?.Dispose();
        _ownedImage = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopAnimation();
        }

        base.Dispose(disposing);
    }
}
