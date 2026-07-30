namespace Wadevo.Services;

using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Turns :shortcode: markers into actual emote images. There are two different, deliberately
/// separate implementations here depending on where the text came from:
///
/// - RenderTrustedTextToHtml: for text the streamer typed themselves (Overlay Designer Text
///   widgets) - baking &lt;img&gt; tags directly into server-generated HTML is safe here because
///   there's no untrusted input involved.
///
/// - The client-side JS helper (EmoteScriptBlock/BuildEmoteMapJson): for Alert/Command
///   messages, which can include viewer-typed chat text via the {message} token. Baking
///   raw HTML server-side there would let a viewer put arbitrary HTML/script into a
///   streamer's own OBS overlay by typing it in chat - a real self-XSS risk. Instead, the
///   client-side JS safely builds a mix of text nodes and &lt;img&gt; elements via the DOM API
///   (never innerHTML string concatenation of untrusted text), so nothing except a
///   recognized, server-controlled emote shortcode can ever become an image - everything
///   else stays inert text no matter what it contains.
/// </summary>
public static class EmoteRenderHelper
{
    private static readonly Regex ShortcodePattern = new(":([a-zA-Z0-9_]+):", RegexOptions.Compiled);

    public static string RenderTrustedTextToHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        IReadOnlyDictionary<string, Models.EmoteModel> emotes = EmoteCache.GetSnapshot();

        if (emotes.Count == 0)
        {
            return WebUtility.HtmlEncode(text);
        }

        // Encode first, then substitute - the shortcode pattern only matches plain
        // alphanumeric/underscore characters, which HTML-encoding never touches, so this
        // can't be used to smuggle anything through the encoding step.
        string encoded = WebUtility.HtmlEncode(text);

        return ShortcodePattern.Replace(encoded, match =>
        {
            string code = match.Groups[1].Value;

            if (!emotes.TryGetValue(code, out Models.EmoteModel? emote))
            {
                return match.Value;
            }

            string safeUrl = WebUtility.HtmlEncode(emote.ImageUrl);
            string safeAlt = WebUtility.HtmlEncode(match.Value);

            return $"<img class=\"wadevo-emote\" src=\"{safeUrl}\" alt=\"{safeAlt}\" style=\"height:1.3em;vertical-align:middle;\">";
        });
    }

    // A JSON object of {shortcode: imageUrl}, embedded into overlay pages that need the
    // client-side-safe rendering path.
    public static string BuildEmoteMapJson()
    {
        // Keys forced to lowercase here, and the lookup code is lowercased client-side to
        // match (see wadevoSetTextWithEmotes below) - the C# dictionary's case-insensitive
        // comparer only affects which entries get treated as duplicates when building this
        // map, not what casing actually survives into the JSON. A JS object's key lookup
        // is always exact-case, so ":feelsgoodman:" would silently fail to match a
        // "FeelsGoodMan" key without this.
        Dictionary<string, string> map = EmoteCache.GetSnapshot()
            .GroupBy(pair => pair.Key.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First().Value.ImageUrl);

        return JsonSerializer.Serialize(map);
    }

    // Shared JS helper, meant to be embedded once per overlay page that needs it. Builds a
    // mix of text nodes and <img> elements via the DOM API - deliberately never uses
    // innerHTML with the raw text, since that text may contain untrusted viewer chat content.
    public static string EmoteScriptBlock()
    {
        return """
        function wadevoSetTextWithEmotes(el, text, emoteMap) {
            el.innerHTML = "";

            var parts = String(text).split(/(:[a-zA-Z0-9_]+:)/g);

            parts.forEach(function (part) {
                var isShortcode = part.length > 2 && part.charAt(0) === ":" && part.charAt(part.length - 1) === ":";
                var code = isShortcode ? part.slice(1, -1).toLowerCase() : null;

                if (code && emoteMap[code]) {
                    var img = document.createElement("img");
                    img.src = emoteMap[code];
                    img.alt = part;
                    img.className = "wadevo-emote";
                    img.style.height = "1.3em";
                    img.style.verticalAlign = "middle";
                    el.appendChild(img);
                } else if (part.length > 0) {
                    el.appendChild(document.createTextNode(part));
                }
            });
        }
        """;
    }
}
