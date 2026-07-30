namespace Wadevo.Models;

public sealed class OverlayAnimationModel
{
    public bool FadeIn { get; set; } = true;
    public bool FadeOut { get; set; } = true;

    public int FadeInMilliseconds { get; set; } = 250;
    public int FadeOutMilliseconds { get; set; } = 250;

    public int DisplayMilliseconds { get; set; } = 4200;

    public bool SlideFromBottom { get; set; } = true;
    public bool ScaleIn { get; set; } = true;

    public bool BlurIn { get; set; } = true;
    public bool PopIn { get; set; } = true;

    public string CssClass
    {
        get
        {
            List<string> classes = new();

            if (FadeIn)
                classes.Add("fade-in");

            if (FadeOut)
                classes.Add("fade-out");

            if (SlideFromBottom)
                classes.Add("slide-up");

            if (ScaleIn)
                classes.Add("scale-in");

            if (BlurIn)
                classes.Add("blur-in");

            if (PopIn)
                classes.Add("pop-in");

            return string.Join(" ", classes);
        }
    }
}