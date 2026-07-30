namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class AssetLibraryModule : WadevoModule
{
    private readonly WadevoScrollablePanel _listPanel = new();
    private readonly WadevoSearchBox _searchBox = new();
    private readonly ToolTip _rowTooltip = new();

    public AssetLibraryModule()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        WadevoGlassCard card = new()
        {
            Dock = DockStyle.Fill,
            AccentColor = WadevoTheme.Colors.Accent,
            Padding = new Padding(24)
        };

        Label header = new()
        {
            Text = "🗂 Asset Library",
            Location = new Point(24, 16),
            Size = new Size(300, 30),
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent
        };

        Label subheader = new()
        {
            Text = "Everything currently in use across Commands, Alerts, and Overlay Designer.",
            Location = new Point(24, 52),
            Size = new Size(600, 22),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _searchBox.Location = new Point(24, 82);
        _searchBox.Size = new Size(320, 38);
        _searchBox.PlaceholderText = "Search assets...";
        _searchBox.SearchTextChanged += (_, _) => RefreshList();

        _listPanel.Location = new Point(24, 132);
        _listPanel.BackColor = Color.Transparent;
        _listPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        card.Resize += (_, _) =>
        {
            ResizeListPanel(card);
            RefreshList();
        };

        card.Controls.Add(header);
        card.Controls.Add(subheader);
        card.Controls.Add(_searchBox);
        card.Controls.Add(_listPanel);

        Controls.Add(card);

        HandleCreated += (_, _) =>
        {
            ResizeListPanel(card);
            RefreshList();
        };
    }

    private void ResizeListPanel(Control card)
    {
        _listPanel.Width = Math.Max(200, card.ClientSize.Width - 48);
        _listPanel.Height = Math.Max(200, card.ClientSize.Height - 156);
    }

    private sealed record AssetEntry(string Path, string Kind, List<string> UsedBy);

    private void RefreshList()
    {
        _listPanel.Content.SuspendLayout();

        foreach (Control control in _listPanel.Content.Controls.Cast<Control>().ToList())
        {
            _listPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        Dictionary<string, AssetEntry> assetsByPath = new(StringComparer.OrdinalIgnoreCase);

        void Track(string? path, string kind, string usedBy)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!assetsByPath.TryGetValue(path, out AssetEntry? entry))
            {
                entry = new AssetEntry(path, kind, new List<string>());
                assetsByPath[path] = entry;
            }

            if (!entry.UsedBy.Contains(usedBy))
            {
                entry.UsedBy.Add(usedBy);
            }
        }

        foreach (CommandModel command in WadevoCommandHub.CommandService.Commands)
        {
            Track(command.MediaFilePath, InferKind(command.MediaFilePath), $"Command: {command.Name}");
        }

        // Alert media used to be tracked here directly (GifPath/SoundPath/BackgroundImagePath
        // on the alert itself), but alerts now design their appearance as a real Overlay
        // Designer layout instead - that layout's own media (background image, Image
        // widgets, etc.) already gets tracked by the preset loop below, so there's nothing
        // alert-specific left to track separately here.

        foreach (WadevoDesignerPresetModel preset in new WadevoDesignerPresetStore().LoadAll())
        {
            Track(preset.BackgroundImagePath, "Image", $"Overlay: {preset.Name}");

            foreach (WadevoDesignerElementState element in preset.Elements)
            {
                Track(element.ImagePath, "Image", $"Overlay: {preset.Name}");
            }
        }

        foreach (string fontName in Wadevo.Services.CustomFontService.GetCustomFontNames())
        {
            Track(fontName, "Font", "Custom uploaded font");
        }

        string search = _searchBox.SearchText.Trim().ToLowerInvariant();

        IEnumerable<AssetEntry> entries = assetsByPath.Values
            .OrderBy(e => e.Kind)
            .ThenBy(e => Path.GetFileName(e.Path), StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(search))
        {
            entries = entries.Where(e =>
                Path.GetFileName(e.Path).ToLowerInvariant().Contains(search) ||
                e.UsedBy.Any(u => u.ToLowerInvariant().Contains(search)));
        }

        List<AssetEntry> entryList = entries.ToList();

        if (entryList.Count == 0)
        {
            Label empty = new()
            {
                Text = "No assets found yet. Upload images, GIFs, sounds, or fonts in Commands, Alerts, or Overlay Designer.",
                Size = new Size(600, 40),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            _listPanel.Content.Controls.Add(empty);
        }

        int itemWidth = Math.Max(200, _listPanel.ClientSize.Width - 20);

        foreach (AssetEntry entry in entryList)
        {
            _listPanel.Content.Controls.Add(BuildRow(entry, itemWidth));
        }

        _listPanel.Content.ResumeLayout();
        _listPanel.RefreshLayout();
    }

    private static string InferKind(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "File";
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();

        return extension switch
        {
            ".gif" or ".png" or ".jpg" or ".jpeg" or ".bmp" => "Image",
            ".mp4" or ".mov" or ".webm" => "Video",
            ".mp3" or ".wav" or ".ogg" => "Sound",
            _ => "File"
        };
    }

    private static string KindIcon(string kind)
    {
        return kind switch
        {
            "Image" => "🖼",
            "Video" => "🎬",
            "Sound" => "🔊",
            "Font" => "🔤",
            _ => "📄"
        };
    }

    private Panel BuildRow(AssetEntry entry, int width)
    {
        Panel row = new()
        {
            Width = width,
            Height = 60,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        Label iconLabel = new()
        {
            Text = KindIcon(entry.Kind),
            Location = new Point(12, 14),
            Size = new Size(32, 32),
            Font = WadevoTheme.Fonts.CardHeader,
            BackColor = Color.Transparent
        };

        _rowTooltip.SetToolTip(iconLabel, entry.Kind + " file");

        string displayName = entry.Kind == "Font"
            ? entry.Path
            : Path.GetFileName(entry.Path);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = entry.Path;
        }

        Label nameLabel = new()
        {
            Text = displayName,
            Location = new Point(52, 8),
            Size = new Size(width - 230, 22),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        Label usedByLabel = new()
        {
            Text = "Used by: " + string.Join(", ", entry.UsedBy),
            Location = new Point(52, 32),
            Size = new Size(width - 230, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            AutoEllipsis = true
        };

        WadevoButton openButton = new()
        {
            ButtonText = entry.Kind == "Font" ? "Open Font Folder" : "Open Folder",
            Location = new Point(width - 170, 14),
            Size = new Size(150, 32),
            AccentColor = WadevoTheme.Colors.Cyan
        };

        openButton.ButtonClicked += (_, _) =>
        {
            try
            {
                // A Font entry's "path" is actually just the font's display name (fonts
                // aren't tracked by file path the way images/sounds are) - this resolves
                // it back to the real installed file so there's something to actually
                // open, instead of the button being permanently disabled with nothing it
                // could do.
                string? folder = entry.Kind == "Font"
                    ? (CustomFontService.TryGetCustomFontFilePath(entry.Path, out string? fontFilePath)
                        ? Path.GetDirectoryName(fontFilePath)
                        : null)
                    : Path.GetDirectoryName(entry.Path);

                if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true
                    });
                }
                else
                {
                    WadevoMessageBox.Show(FindForm(), "That file's folder couldn't be found.", "Not Found");
                }
            }
            catch
            {
                WadevoMessageBox.Show(FindForm(), "Couldn't open that folder.", "Error");
            }
        };

        row.Controls.Add(iconLabel);
        row.Controls.Add(nameLabel);
        row.Controls.Add(usedByLabel);
        row.Controls.Add(openButton);

        return row;
    }
}
