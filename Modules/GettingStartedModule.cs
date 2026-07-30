namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;

public sealed class GettingStartedModule : WadevoModule
{
    public override string ModuleName => "Getting Started";
    public override string ModuleDescription => "New here? Start with these steps.";

    public event Action<string>? OpenRequested;

    public GettingStartedModule()
    {
        Padding = new Padding(0);

        WadevoScrollablePanel scrollPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(WadevoTheme.Sizes.SpaceXL, WadevoTheme.Sizes.SpaceM, WadevoTheme.Sizes.SpaceXL, WadevoTheme.Sizes.SpaceM),
            BackColor = Color.Transparent
        };

        WadevoGlassCard welcomeCard = BuildWelcomeCard();
        welcomeCard.Margin = new Padding(0, 0, 0, WadevoTheme.Sizes.SpaceM);

        WadevoGlassCard step1 = BuildStepCard(
            "1", "Connect Blaze and/or Twitch",
            "Link your Blaze and/or Twitch account so chat, commands, raids, gifts, and alerts can all reach Wadevo.",
            "Connections", WadevoTheme.Colors.Purple);

        WadevoGlassCard step2 = BuildStepCard(
            "2", "Point Wadevo at your DJ software",
            "Serato or VirtualDJ - pick which one you use in Settings, and Wadevo knows what song is currently playing.",
            "Settings", WadevoTheme.Colors.Cyan);

        WadevoGlassCard step3 = BuildStepCard(
            "3", "Add a free Giphy key",
            "Search and send GIFs to your stream. Get a free key at developers.giphy.com, then paste it in.",
            "GIFs", WadevoTheme.Colors.Accent);

        WadevoGlassCard step4 = BuildStepCard(
            "4", "Build your first command",
            "Create a chat command, an alert, or a GIF command - all through the same guided builder.",
            "Commands", WadevoTheme.Colors.Purple);

        WadevoGlassCard step5 = BuildStepCard(
            "5", "Design your overlay",
            "Arrange your Song ID overlay, then copy its URL into OBS as a Browser Source.",
            "Overlay Engine", WadevoTheme.Colors.Cyan);

        WadevoGlassCard step6 = BuildStepCard(
            "6", "Connect OBS for scene control",
            "In OBS: Tools > WebSocket Server Settings > enable the server, copy the password. " +
            "Then open OBS Studio in Connections and paste it in - lets Wadevo switch scenes, " +
            "and auto-starts your chat/alerts the moment you go live. Add a Scene Switcher panel " +
            "in Workspace Studio to bind scenes to hotkeys that work even mid-game.",
            "Connections", WadevoTheme.Colors.Success);

        foreach (WadevoGlassCard step in new[] { step1, step2, step3, step4, step5, step6 })
        {
            step.Margin = new Padding(0, 0, 0, WadevoTheme.Sizes.SpaceS);
        }

        WadevoGlassCard botTip = BuildBotIdentityTipCard();
        botTip.Margin = new Padding(0, WadevoTheme.Sizes.SpaceS, 0, 0);

        WadevoGlassCard permissionsTip = BuildCommandPermissionsTipCard();
        permissionsTip.Margin = new Padding(0, WadevoTheme.Sizes.SpaceS, 0, 0);

        WadevoGlassCard variablesCard = BuildVariablesReferenceCard();
        variablesCard.Margin = new Padding(0, WadevoTheme.Sizes.SpaceM, 0, 0);

        WadevoGlassCard emotesCard = BuildEmotesReferenceCard();
        emotesCard.Margin = new Padding(0, WadevoTheme.Sizes.SpaceM, 0, 0);

        WadevoGlassCard pageGuideCard = BuildPageGuideCard();
        pageGuideCard.Margin = new Padding(0, WadevoTheme.Sizes.SpaceM, 0, 0);

        scrollPanel.Content.Controls.Add(welcomeCard);
        scrollPanel.Content.Controls.Add(step1);
        scrollPanel.Content.Controls.Add(step2);
        scrollPanel.Content.Controls.Add(step3);
        scrollPanel.Content.Controls.Add(step4);
        scrollPanel.Content.Controls.Add(step5);
        scrollPanel.Content.Controls.Add(step6);
        scrollPanel.Content.Controls.Add(botTip);
        scrollPanel.Content.Controls.Add(permissionsTip);
        scrollPanel.Content.Controls.Add(variablesCard);
        scrollPanel.Content.Controls.Add(emotesCard);
        scrollPanel.Content.Controls.Add(pageGuideCard);

        Controls.Add(scrollPanel);

        scrollPanel.RefreshLayout();
    }

    private WadevoGlassCard BuildVariablesReferenceCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            AccentColor = WadevoTheme.Colors.Cyan,
            ShowGlow = false,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🏷️ Variables & Tokens",
            Location = new Point(24, 20),
            Size = new Size(400, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        Label intro = new()
        {
            Text = "When you're writing an alert message, you'll see things like {username} in the " +
                   "text box. These aren't typos - they're placeholders that get swapped out for real " +
                   "info automatically. Here's what each one actually does.",
            Location = new Point(24, 52),
            Size = new Size(700, 44),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        (string Token, string Meaning, string Example, string WorksIn)[] variables =
        {
            ("{username}", "The person who triggered this - who followed, subscribed, gifted, voted, or requested the song.",
                "\"Thanks for following, PixelWarrior23!\"", "Alerts, Song Request confirmations"),
            ("{message}", "Whatever the viewer typed in chat. Only meaningful when the alert's trigger is a chat message.",
                "\"They said: hey everyone!\"", "Alerts (Chat Message trigger)"),
            ("{viewerCount}", "How many people were watching at the moment of a raid.",
                "\"...raided with 42 viewers!\"", "Alerts (Raid trigger)"),
            ("{giftCount}", "How many subs were gifted at once.",
                "\"Gifted 5 subs!\"", "Alerts (Gift Subs trigger)"),
            ("{voteAmount}", "The number tied to a vote or poll result.",
                "\"...voted 30 points!\"", "Alerts (Vote trigger)"),
            ("{song}", "The song a viewer requested.",
                "\"Added Blinding Lights to the queue!\"", "Song Request confirmation message")
        };

        int y = 104;

        foreach ((string token, string meaning, string example, string worksIn) in variables)
        {
            y = AddVariableRow(card, token, meaning, example, worksIn, y);
        }

        Label commandsNote = new()
        {
            Text = "⚠️ Commands don't support any of these yet. Whatever you type in a Command's " +
                   "response gets sent exactly as written, every single time - no swapping in the " +
                   "viewer's name or anything else.",
            Location = new Point(24, y + 8),
            Size = new Size(700, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Warning,
            BackColor = Color.Transparent
        };

        card.Height = y + 60;

        card.Controls.Add(header);
        card.Controls.Add(intro);
        card.Controls.Add(commandsNote);

        return card;
    }

    private int AddVariableRow(WadevoGlassCard card, string token, string meaning, string example, string worksIn, int y)
    {
        Panel row = new()
        {
            Location = new Point(24, y),
            Size = new Size(700, 76),
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label tokenLabel = new()
        {
            Text = token,
            Location = new Point(12, 10),
            Size = new Size(160, 22),
            Font = new Font("Consolas", 10.5F, FontStyle.Bold),
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        Label meaningLabel = new()
        {
            Text = meaning,
            Location = new Point(180, 8),
            Size = new Size(510, 36),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label exampleLabel = new()
        {
            Text = "e.g. " + example,
            Location = new Point(12, 48),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        Label worksInLabel = new()
        {
            Text = "Works in: " + worksIn,
            Location = new Point(180, 48),
            Size = new Size(510, 18),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        row.Controls.Add(tokenLabel);
        row.Controls.Add(meaningLabel);
        row.Controls.Add(exampleLabel);
        row.Controls.Add(worksInLabel);

        card.Controls.Add(row);

        return y + 84;
    }

    private WadevoGlassCard BuildEmotesReferenceCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            AccentColor = WadevoTheme.Colors.Success,
            ShowGlow = false,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "😀 Emotes",
            Location = new Point(24, 20),
            Size = new Size(400, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Success,
            BackColor = Color.Transparent
        };

        Label intro = new()
        {
            Text = "You can drop emotes into overlay text, alert messages, and command responses by " +
                   "wrapping a name in colons, like :FeelsGoodMan:. It looks like a typo until Wadevo " +
                   "swaps it for the actual image wherever that text is shown on stream. You don't have " +
                   "to memorize any names, though - anywhere you see the 😀 button next to a text box, " +
                   "click it to search and insert one instead of typing it by hand.",
            Location = new Point(24, 54),
            Size = new Size(700, 88),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Panel exampleRow = new()
        {
            Location = new Point(24, 150),
            Size = new Size(700, 70),
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label typedLabel = new()
        {
            Text = "You type:",
            Location = new Point(14, 10),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label typedValue = new()
        {
            Text = "Thanks for the follow :FeelsGoodMan:",
            Location = new Point(14, 32),
            Size = new Size(340, 26),
            Font = new Font("Consolas", 12F, FontStyle.Bold),
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        Label shownLabel = new()
        {
            Text = "Viewers see:",
            Location = new Point(370, 10),
            Size = new Size(160, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label shownValue = new()
        {
            Text = "Thanks for the follow [🙂]",
            Location = new Point(370, 32),
            Size = new Size(320, 26),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        exampleRow.Controls.Add(typedLabel);
        exampleRow.Controls.Add(typedValue);
        exampleRow.Controls.Add(shownLabel);
        exampleRow.Controls.Add(shownValue);

        Label sourcesNote = new()
        {
            Text = "Two sources feed the picker: a library of well-known emotes (FeelsGoodMan, " +
                   "monkaS, and other classics) plus any custom emote you upload yourself from that " +
                   "same picker - upload an image, give it a name, and it's yours to reuse anywhere.",
            Location = new Point(24, 232),
            Size = new Size(700, 55),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label scopeNote = new()
        {
            Text = "⚠️ Emotes only render inside Wadevo's own overlays (Alerts, Command output, Song " +
                   "ID, Overlay Designer text). They won't show as images in your real chat " +
                   "or your stream title - those are plain text everywhere, on every platform.",
            Location = new Point(24, 294),
            Size = new Size(700, 55),
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.Warning,
            BackColor = Color.Transparent
        };

        card.Height = 372;

        card.Controls.Add(header);
        card.Controls.Add(intro);
        card.Controls.Add(exampleRow);
        card.Controls.Add(sourcesNote);
        card.Controls.Add(scopeNote);

        return card;
    }

    private WadevoGlassCard BuildPageGuideCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            AccentColor = WadevoTheme.Colors.Purple,
            ShowGlow = false,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🗺️ What Each Page Does",
            Location = new Point(24, 20),
            Size = new Size(400, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Purple,
            BackColor = Color.Transparent
        };

        (string Page, string Explanation)[] pages =
        {
            ("Connections", "Log into Blaze and/or Twitch here once, and every other page can use it - chat, alerts, commands, all of it. Also where OBS connects, for scene control and hotkeys. Always your first stop."),
            ("Commands", "Chat-triggered actions - someone types !something in chat, something happens. Post a message, show a GIF, play a sound. You choose who's allowed to use each one, and how often."),
            ("Song Requests", "Let viewers request songs with a chat command. Manage the live queue - mark songs played, remove ones you skip."),
            ("GIFs", "Search Giphy and send a GIF straight to your live overlay, right now."),
            ("Soundboard", "Play sound clips on stream with a click or a global hotkey - works even when Wadevo isn't the focused window."),
            ("Overlay Designer", "Build what actually shows up on stream - song info, custom text, images, clocks, countdowns. Drag things around, style them, save as many different looks as you want."),
            ("Asset Library", "See everything you've uploaded across the whole app - images, fonts, sounds - and exactly where each one is being used."),
            ("Workspace Studio", "A separate, customizable window for the things you check most while live - commands, chat, song requests, OBS scene switching - without digging through the main app mid-stream."),
            ("Alerts", "On-screen pop-ups for follows, subs, raids, and more. Fully custom - your colors, your font, your animation, your wording."),
            ("The Booth", "Pick which of your saved overlays is actually live right now, and see your connection status at a glance."),
            ("Overlay Engine", "The URLs you paste into OBS as Browser Sources, including the Combined Chat overlay (chat from every connected platform, merged into one feed). If you're not sure what to paste into OBS, this is where to look."),
            ("Settings", "App-wide preferences - which DJ software Wadevo reads (Serato or VirtualDJ), backups, and the app's changelog."),
            ("Dashboard", "Today's numbers at a glance - followers, subs, raids, commands used - plus a running feed of what just happened."),
            ("Support Wadevo", "Wadevo is free. If you want to support it anyway, this is where.")
        };

        int y = 60;

        foreach ((string page, string explanation) in pages)
        {
            y = AddPageGuideRow(card, page, explanation, y);
        }

        card.Height = y + 24;

        card.Controls.Add(header);

        return card;
    }

    private int AddPageGuideRow(WadevoGlassCard card, string page, string explanation, int y)
    {
        Label pageLabel = new()
        {
            Text = page,
            Location = new Point(24, y),
            Size = new Size(160, 40),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label explanationLabel = new()
        {
            Text = explanation,
            Location = new Point(196, y),
            Size = new Size(540, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        card.Controls.Add(pageLabel);
        card.Controls.Add(explanationLabel);

        return y + 52;
    }

    private WadevoGlassCard BuildBotIdentityTipCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            Height = 130,
            AccentColor = WadevoTheme.Colors.Purple,
            ShowGlow = true,
            Padding = new Padding(0)
        };

        Label icon = new()
        {
            Text = "🤖",
            Location = new Point(20, 24),
            Size = new Size(48, 48),
            Font = WadevoTheme.Fonts.Hero,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label titleLabel = new()
        {
            Text = "Give Wadevo its own voice",
            Location = new Point(84, 14),
            Size = new Size(440, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label descriptionLabel = new()
        {
            Text = "By default, commands post under your own name. Optional: connect a separate " +
                   "Blaze and/or Twitch account as a real bot identity, so commands post as " +
                   "\"your bot,\" not you - set up per platform in its own Connections popup.",
            Location = new Point(84, 42),
            Size = new Size(650, 50),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton openButton = new()
        {
            ButtonText = "Go to Connections →",
            Location = new Point(84, 96),
            Size = new Size(200, 30),
            AccentColor = WadevoTheme.Colors.Purple
        };

        openButton.ButtonClicked += (_, _) => OpenRequested?.Invoke("Connections");

        card.Controls.Add(icon);
        card.Controls.Add(titleLabel);
        card.Controls.Add(descriptionLabel);
        card.Controls.Add(openButton);

        return card;
    }

    private WadevoGlassCard BuildCommandPermissionsTipCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            Height = 130,
            AccentColor = WadevoTheme.Colors.Warning,
            ShowGlow = true,
            Padding = new Padding(0)
        };

        Label icon = new()
        {
            Text = "🔒",
            Location = new Point(20, 24),
            Size = new Size(48, 48),
            Font = WadevoTheme.Fonts.Hero,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label titleLabel = new()
        {
            Text = "Not every command should be open to everyone",
            Location = new Point(84, 14),
            Size = new Size(600, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label descriptionLabel = new()
        {
            Text = "Anything powerful (like switching OBS scenes) should be locked down. When building " +
                   "or editing a command, set \"Minimum Role\" to Moderator (you and your mods) or " +
                   "Owner (only you, not even mods) - everyone else typing the trigger gets silently ignored.",
            Location = new Point(84, 42),
            Size = new Size(650, 50),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton openButton = new()
        {
            ButtonText = "Go to Commands →",
            Location = new Point(84, 96),
            Size = new Size(200, 30),
            AccentColor = WadevoTheme.Colors.Warning
        };

        openButton.ButtonClicked += (_, _) => OpenRequested?.Invoke("Commands");

        card.Controls.Add(icon);
        card.Controls.Add(titleLabel);
        card.Controls.Add(descriptionLabel);
        card.Controls.Add(openButton);

        return card;
    }

    private WadevoGlassCard BuildWelcomeCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            Height = 90,
            AccentColor = WadevoTheme.Colors.Accent,
            ShowGlow = true,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = $"👋  Welcome to {WadevoBrand.AppName}",
            Location = new Point(24, 16),
            Size = new Size(500, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "Five quick steps and you're fully set up. Come back to this page anytime from the sidebar.",
            Location = new Point(24, 48),
            Size = new Size(700, 24),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        card.Controls.Add(header);
        card.Controls.Add(description);

        return card;
    }

    private WadevoGlassCard BuildStepCard(
        string number,
        string title,
        string description,
        string targetPage,
        Color accentColor)
    {
        WadevoGlassCard card = new()
        {
            Width = 760,
            Height = 100,
            AccentColor = accentColor,
            ShowGlow = false,
            Padding = new Padding(0)
        };

        Label numberLabel = new()
        {
            Text = number,
            Location = new Point(20, 24),
            Size = new Size(48, 48),
            Font = WadevoTheme.Fonts.Hero,
            ForeColor = accentColor,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label titleLabel = new()
        {
            Text = title,
            Location = new Point(84, 18),
            Size = new Size(440, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label descriptionLabel = new()
        {
            Text = description,
            Location = new Point(84, 46),
            Size = new Size(430, 44),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton openButton = new()
        {
            ButtonText = $"Go to {targetPage} →",
            Location = new Point(530, 32),
            Size = new Size(210, 36),
            AccentColor = accentColor
        };

        openButton.ButtonClicked += (_, _) => OpenRequested?.Invoke(targetPage);

        card.Controls.Add(numberLabel);
        card.Controls.Add(titleLabel);
        card.Controls.Add(descriptionLabel);
        card.Controls.Add(openButton);

        return card;
    }
}
