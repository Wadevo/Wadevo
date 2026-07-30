namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class SettingsModule : WadevoModule
{
    private readonly WadevoAppSettingsStore _settingsStore = new();

    public override string ModuleName => "Settings";
    public override string ModuleDescription => "App-wide preferences and connections.";

    public SettingsModule()
    {
        Padding = new Padding(0);

        WadevoScrollablePanel scrollPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(WadevoTheme.Sizes.SpaceXL, WadevoTheme.Sizes.SpaceM, WadevoTheme.Sizes.SpaceXL, WadevoTheme.Sizes.SpaceM),
            BackColor = Color.Transparent
        };

        WadevoGlassCard seratoCard = BuildSeratoCard();
        WadevoGlassCard virtualDjCard = BuildVirtualDjCard();
        WadevoGlassCard backupCard = BuildBackupCard();
        WadevoGlassCard aboutCard = BuildAboutCard();

        seratoCard.Margin = new Padding(0, 0, 0, WadevoTheme.Sizes.SpaceM);
        virtualDjCard.Margin = new Padding(0, 0, 0, WadevoTheme.Sizes.SpaceM);
        backupCard.Margin = new Padding(0, 0, 0, WadevoTheme.Sizes.SpaceM);
        aboutCard.Margin = new Padding(0);

        scrollPanel.Content.Controls.Add(seratoCard);
        scrollPanel.Content.Controls.Add(virtualDjCard);
        scrollPanel.Content.Controls.Add(backupCard);
        scrollPanel.Content.Controls.Add(aboutCard);

        Controls.Add(scrollPanel);

        scrollPanel.RefreshLayout();
    }

    private WadevoGlassCard BuildSeratoCard()
    {
        WadevoAppSettingsModel settings = _settingsStore.Load();

        // The local-history-file method (SeratoHistoryReader) is fully built and still
        // wired into MainForm - it's just not offered as a UI choice right now, since
        // Serato DJ 4.0 has an acknowledged bug where History sometimes doesn't get
        // written to disk at all, which would silently strand anyone who picked it. Once
        // that's fixed on Serato's end, re-adding the method-selector buttons here (see
        // git history / conversation for the exact removed code) makes it available again
        // with zero other changes needed.
        if (!settings.SeratoReadMethod.Equals("LivePlaylistUrl", StringComparison.OrdinalIgnoreCase))
        {
            settings.SeratoReadMethod = "LivePlaylistUrl";
            _settingsStore.Save(settings);
        }

        WadevoGlassCard card = new()
        {
            Width = 700,
            Height = 202,
            AccentColor = WadevoTheme.Colors.Cyan,
            ShowGlow = false,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🎧  Serato",
            Location = new Point(24, 20),
            Size = new Size(300, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "The playlist URL Wadevo reads to know what song is currently playing.",
            Location = new Point(24, 50),
            Size = new Size(600, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoLabeledTextBox urlBox = new()
        {
            LabelText = "Serato playlist URL",
            TextValue = settings.SeratoPlaylistUrl,
            Location = new Point(24, 80),
            Size = new Size(520, 58)
        };

        WadevoButton saveButton = new()
        {
            ButtonText = "💾 Save",
            Location = new Point(560, 108),
            Size = new Size(110, 30),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        Label savedLabel = new()
        {
            Text = "",
            Location = new Point(24, 172),
            Size = new Size(600, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Success,
            BackColor = Color.Transparent
        };

        WadevoCheckBox activeToggle = new()
        {
            Text = "Use Serato as my DJ software",
            Location = new Point(24, 142),
            Size = new Size(400, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.DjSoftware.Equals("Serato", StringComparison.OrdinalIgnoreCase)
        };

        activeToggle.CheckedChanged += (_, _) =>
        {
            WadevoAppSettingsModel current = _settingsStore.Load();
            current.DjSoftware = activeToggle.Checked ? "Serato" : "VirtualDJ";
            _settingsStore.Save(current);

            savedLabel.Text = "Saved. Takes effect within a few seconds - no restart needed.";
        };

        saveButton.ButtonClicked += (_, _) =>
        {
            WadevoAppSettingsModel current = _settingsStore.Load();
            current.SeratoPlaylistUrl = urlBox.TextValue.Trim();
            _settingsStore.Save(current);

            savedLabel.Text = "Saved. Takes effect within a few seconds - no restart needed.";
        };

        card.Controls.Add(header);
        card.Controls.Add(description);
        card.Controls.Add(urlBox);
        card.Controls.Add(saveButton);
        card.Controls.Add(activeToggle);
        card.Controls.Add(savedLabel);

        return card;
    }

    private WadevoGlassCard BuildVirtualDjCard()
    {
        WadevoAppSettingsModel settings = _settingsStore.Load();

        WadevoGlassCard card = new()
        {
            Width = 700,
            Height = 268,
            AccentColor = WadevoTheme.Colors.Purple,
            ShowGlow = false,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🎚  VirtualDJ",
            Location = new Point(24, 20),
            Size = new Size(300, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "Wadevo reads VirtualDJ's own local history file - no login or setup needed " +
                   "beyond the two settings below, changed once inside VirtualDJ itself.",
            Location = new Point(24, 50),
            Size = new Size(640, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label setupHeader = new()
        {
            Text = "One-time setup in VirtualDJ",
            Location = new Point(24, 96),
            Size = new Size(400, 22),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent
        };

        Label setupSteps = new()
        {
            Text = "1. Open Options (the cog icon) > Settings, and search for \"tracklistFormat\" - " +
                   "set it to exactly:  %author% - %title%\n" +
                   "2. Search for \"historyDelay\" and lower it (e.g. 5-10 seconds) so Wadevo " +
                   "picks up track changes faster - VirtualDJ's default is 45 seconds.",
            Location = new Point(24, 120),
            Size = new Size(640, 50),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label pathLabel = new()
        {
            Text = $"Reading from: {new VirtualDjNowPlayingReader().FilePath}",
            Location = new Point(24, 178),
            Size = new Size(640, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoCheckBox activeToggle2 = new()
        {
            Text = "Use VirtualDJ as my DJ software",
            Location = new Point(24, 208),
            Size = new Size(400, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = settings.DjSoftware.Equals("VirtualDJ", StringComparison.OrdinalIgnoreCase)
        };

        Label savedLabel2 = new()
        {
            Text = "",
            Location = new Point(24, 238),
            Size = new Size(600, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.Success,
            BackColor = Color.Transparent
        };

        activeToggle2.CheckedChanged += (_, _) =>
        {
            WadevoAppSettingsModel current = _settingsStore.Load();
            current.DjSoftware = activeToggle2.Checked ? "VirtualDJ" : "Serato";
            _settingsStore.Save(current);

            savedLabel2.Text = "Saved. Takes effect within a few seconds - no restart needed.";
        };

        card.Controls.Add(header);
        card.Controls.Add(description);
        card.Controls.Add(setupHeader);
        card.Controls.Add(setupSteps);
        card.Controls.Add(pathLabel);
        card.Controls.Add(activeToggle2);
        card.Controls.Add(savedLabel2);

        return card;
    }

    private WadevoGlassCard BuildBackupCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 700,
            Height = 220,
            AccentColor = WadevoTheme.Colors.Accent,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "💾 Backup & Restore",
            Location = new Point(24, 20),
            Size = new Size(400, 30),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        Label description = new()
        {
            Text = "Everything you've built - commands, alerts, overlays, custom fonts - lives only on " +
                   "this computer right now. Export a backup to protect it, or move it to a new machine.",
            Location = new Point(24, 54),
            Size = new Size(640, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoCheckBox includeCredentialsBox = new()
        {
            Text = "Include Blaze/Twitch/API credentials (not recommended for sharing - won't work on a different computer anyway)",
            Location = new Point(24, 102),
            Size = new Size(640, 40),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted
        };

        WadevoButton exportButton = new()
        {
            ButtonText = "📤 Export Backup...",
            Location = new Point(24, 150),
            Size = new Size(200, 40),
            AccentColor = WadevoTheme.Colors.Accent
        };

        WadevoButton importButton = new()
        {
            ButtonText = "📥 Import Backup...",
            Location = new Point(236, 150),
            Size = new Size(200, 40),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        Label statusLabel = new()
        {
            Text = "",
            Location = new Point(24, 196),
            Size = new Size(640, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        exportButton.ButtonClicked += (_, _) =>
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Save Wadevo Backup",
                Filter = "Wadevo backup (*.wadevobackup)|*.wadevobackup",
                FileName = $"Wadevo Backup {DateTime.Now:yyyy-MM-dd}.wadevobackup"
            };

            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            {
                return;
            }

            BackupExportResult result = WadevoBackupService.ExportBackup(
                dialog.FileName, includeCredentialsBox.Checked);

            if (result.Success)
            {
                statusLabel.ForeColor = WadevoTheme.Colors.Success;
                statusLabel.Text = $"✅ Backup saved - {result.IncludedFiles.Count} item(s) included.";
            }
            else
            {
                statusLabel.ForeColor = WadevoTheme.Colors.Error;
                statusLabel.Text = $"❌ Backup failed: {result.ErrorMessage}";
            }
        };

        importButton.ButtonClicked += (_, _) =>
        {
            bool confirmed = WadevoMessageBox.Confirm(
                FindForm(),
                "Restoring a backup will overwrite your current commands, alerts, overlays, and settings " +
                "with what's in the backup file. This can't be undone. Continue?",
                "Restore Backup");

            if (!confirmed)
            {
                return;
            }

            using OpenFileDialog dialog = new()
            {
                Title = "Choose a Wadevo Backup",
                Filter = "Wadevo backup (*.wadevobackup)|*.wadevobackup|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            {
                return;
            }

            BackupImportResult result = WadevoBackupService.ImportBackup(dialog.FileName);

            if (result.Success)
            {
                statusLabel.ForeColor = WadevoTheme.Colors.Success;
                statusLabel.Text = $"✅ Restored {result.RestoredFiles.Count} item(s). Restart Wadevo for everything to take effect.";

                WadevoMessageBox.Show(
                    FindForm(),
                    "Backup restored. Please close and reopen Wadevo so everything loads correctly.",
                    "Restore Complete");
            }
            else
            {
                statusLabel.ForeColor = WadevoTheme.Colors.Error;
                statusLabel.Text = $"❌ Restore failed: {result.ErrorMessage}";
            }
        };

        card.Controls.Add(header);
        card.Controls.Add(description);
        card.Controls.Add(includeCredentialsBox);
        card.Controls.Add(exportButton);
        card.Controls.Add(importButton);
        card.Controls.Add(statusLabel);

        return card;
    }

    private static WadevoGlassCard BuildAboutCard()
    {
        WadevoGlassCard card = new()
        {
            Width = 700,
            Height = 150,
            AccentColor = WadevoTheme.Colors.TextMuted,
            ShowGlow = false,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = $"ℹ️  About {WadevoBrand.AppName}",
            Location = new Point(24, 20),
            Size = new Size(300, 28),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label versionLabel = new()
        {
            Text = $"{WadevoBrand.AppName} {WadevoBrand.Version} — {WadevoBrand.Tagline}",
            Location = new Point(24, 54),
            Size = new Size(500, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton whatsNewButton = new()
        {
            ButtonText = "📋 What's New",
            Location = new Point(24, 82),
            Size = new Size(160, 34),
            AccentColor = WadevoTheme.Colors.Accent
        };

        whatsNewButton.ButtonClicked += (_, _) =>
        {
            using WadevoChangelogForm form = new();
            form.ShowDialog(card.FindForm());
        };

        card.Controls.Add(header);
        card.Controls.Add(versionLabel);
        card.Controls.Add(whatsNewButton);

        return card;
    }
}
