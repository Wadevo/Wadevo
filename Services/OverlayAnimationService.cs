namespace Wadevo.Services;

using Wadevo.Models;

public sealed class OverlayAnimationService
{
    public string BuildCss(OverlayAnimationModel animation)
    {
        return
            ".overlay {\n" +
            "    opacity: 0;\n" +
            $"    transform: {GetInitialTransform(animation)};\n" +
            "    filter: blur(4px);\n" +
            "    will-change: opacity, transform, filter;\n" +
            "}\n\n" +
            ".overlay.visible {\n" +
            "    opacity: 1;\n" +
            "    transform: translateY(0) scale(1);\n" +
            "    filter: blur(0);\n" +
            "    transition:\n" +
            $"        opacity {animation.FadeInMilliseconds}ms cubic-bezier(.2,.9,.2,1),\n" +
            $"        transform {animation.FadeInMilliseconds}ms cubic-bezier(.2,.9,.2,1),\n" +
            $"        filter {animation.FadeInMilliseconds}ms ease;\n" +
            "}\n\n" +
            ".overlay.hiding {\n" +
            "    opacity: 0;\n" +
            $"    transform: {GetExitTransform(animation)};\n" +
            "    filter: blur(3px);\n" +
            "    transition:\n" +
            $"        opacity {animation.FadeOutMilliseconds}ms ease,\n" +
            $"        transform {animation.FadeOutMilliseconds}ms ease,\n" +
            $"        filter {animation.FadeOutMilliseconds}ms ease;\n" +
            "}\n\n" +
            ".overlay.visible .alert-card {\n" +
            "    animation: wadevo-alert-pop 520ms cubic-bezier(.2,.9,.2,1);\n" +
            "}\n\n" +
            "@keyframes wadevo-alert-pop {\n" +
            "    0% { transform: scale(.94); }\n" +
            "    65% { transform: scale(1.025); }\n" +
            "    100% { transform: scale(1); }\n" +
            "}\n";
    }

    public string BuildJavaScript(OverlayAnimationModel animation)
    {
        return
            "function showOverlay() {\n" +
            "    overlay.classList.remove(\"visible\");\n" +
            "    overlay.classList.remove(\"hiding\");\n\n" +
            "    void overlay.offsetWidth;\n\n" +
            "    overlay.classList.add(\"visible\");\n\n" +
            "    clearTimeout(window.hideOverlayTimer);\n\n" +
            "    window.hideOverlayTimer = setTimeout(() => {\n" +
            "        overlay.classList.remove(\"visible\");\n" +
            "        overlay.classList.add(\"hiding\");\n" +
            $"    }}, {animation.DisplayMilliseconds});\n" +
            "}\n";
    }

    private static string GetInitialTransform(OverlayAnimationModel animation)
    {
        string translate = animation.SlideFromBottom
            ? "translateY(28px)"
            : "translateY(0)";

        string scale = animation.ScaleIn
            ? "scale(.92)"
            : "scale(1)";

        return $"{translate} {scale}";
    }

    private static string GetExitTransform(OverlayAnimationModel animation)
    {
        string translate = animation.SlideFromBottom
            ? "translateY(-16px)"
            : "translateY(0)";

        string scale = animation.ScaleIn
            ? "scale(.98)"
            : "scale(1)";

        return $"{translate} {scale}";
    }
}