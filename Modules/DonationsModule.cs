namespace Wadevo.Modules;

using System.Diagnostics;
using Wadevo.Controls;
using Wadevo.Core;

public sealed class DonationsModule : WadevoModule
{
    public override string ModuleName => "Support Wadevo";
    public override string ModuleDescription => "Wadevo is free. If it's helped your stream, a donation is always welcome.";

    public DonationsModule()
    {
        Padding = new Padding(0);

        WadevoGlassCard card = new()
        {
            Dock = DockStyle.Top,
            Height = 340,
            AccentColor = WadevoTheme.Colors.Accent,
            Padding = new Padding(32)
        };

        Label header = new()
        {
            Text = "💚 Thank You for Using Wadevo",
            Location = new Point(32, 24),
            Size = new Size(600, 36),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        Label body = new()
        {
            Text = "Wadevo is, and always will be, free to use. It's built by a streamer, for streamers - " +
                   "no subscriptions, no locked features, no ads. If it's made your stream better and you'd " +
                   "like to support development, a donation is genuinely appreciated but never required.",
            Location = new Point(32, 70),
            Size = new Size(680, 80),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label linksHeader = new()
        {
            Text = "Ways to support",
            Location = new Point(32, 160),
            Size = new Size(400, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        WadevoButton koFiButton = CreateSupportButton("☕ Ko-fi", WadevoTheme.Colors.Cyan, new Point(32, 196), "https://ko-fi.com/wadevo");
        WadevoButton paypalButton = CreateSupportButton("💙 PayPal", WadevoTheme.Colors.Purple, new Point(232, 196), "https://paypal.me/Wadevo");
        WadevoButton patreonButton = CreateSupportButton("🧡 Patreon", WadevoTheme.Colors.Warning, new Point(432, 196), "https://www.patreon.com/cw/Wadevo");

        card.Controls.Add(header);
        card.Controls.Add(body);
        card.Controls.Add(linksHeader);
        card.Controls.Add(koFiButton);
        card.Controls.Add(paypalButton);
        card.Controls.Add(patreonButton);

        Controls.Add(card);
    }

    private static WadevoButton CreateSupportButton(string text, Color color, Point location, string url)
    {
        WadevoButton button = new()
        {
            ButtonText = text,
            Location = location,
            Size = new Size(180, 42),
            AccentColor = color
        };

        button.ButtonClicked += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                WadevoMessageBox.Show(
                    button.FindForm(),
                    "Couldn't open the link. You can visit it directly:\n" + url,
                    "Link Error");
            }
        };

        return button;
    }
}
