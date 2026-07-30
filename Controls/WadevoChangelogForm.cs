namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoChangelogForm : WadevoDialogForm
{
    public WadevoChangelogForm()
        : base($"What's New in {WadevoBrand.AppName}")
    {
        Size = new Size(560, 520);

        WadevoScrollablePanel scrollPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };

        ContentPanel.Controls.Add(scrollPanel);

        int y = 8;

        foreach (ChangelogEntry entry in WadevoChangelog.Entries)
        {
            Label versionLabel = new()
            {
                Text = entry.Version,
                Location = new Point(8, y),
                Size = new Size(300, 28),
                Font = WadevoTheme.Fonts.CardHeader,
                ForeColor = WadevoTheme.Colors.Accent,
                BackColor = Color.Transparent
            };

            scrollPanel.Content.Controls.Add(versionLabel);
            y += 36;

            foreach (string change in entry.Changes)
            {
                Label bullet = new()
                {
                    Text = "•",
                    Location = new Point(10, y),
                    Size = new Size(16, 20),
                    Font = WadevoTheme.Fonts.Default,
                    ForeColor = WadevoTheme.Colors.TextMuted,
                    BackColor = Color.Transparent
                };

                Label changeLabel = new()
                {
                    Text = change,
                    Location = new Point(30, y),
                    Size = new Size(480, 40),
                    Font = WadevoTheme.Fonts.Small,
                    ForeColor = WadevoTheme.Colors.Text,
                    BackColor = Color.Transparent,
                    AutoSize = false
                };

                scrollPanel.Content.Controls.Add(bullet);
                scrollPanel.Content.Controls.Add(changeLabel);

                int estimatedLines = Math.Max(1, (int)Math.Ceiling(change.Length / 68.0));
                y += 20 * estimatedLines + 4;
            }

            y += 20;
        }

        scrollPanel.Content.Size = new Size(520, y + 20);
        scrollPanel.RefreshLayout();

        WadevoButton closeButton = new()
        {
            ButtonText = "Close",
            Size = new Size(90, 36),
            AccentColor = WadevoTheme.Colors.TextMuted,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        closeButton.Location = new Point(Width - 130, Height - 90);
        closeButton.ButtonClicked += (_, _) => Close();

        Controls.Add(closeButton);
        closeButton.BringToFront();
    }
}
