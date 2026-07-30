namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class WorkspaceStudioForm : Form
{
    private readonly WorkspaceLayoutStore _layoutStore = new();
    private WorkspaceLayoutModel _layout = new();

    private readonly Panel _canvas = new();
    private readonly WadevoButton _addPanelButton = new();
    private readonly WadevoButton _alwaysOnTopButton = new();
    private readonly Dictionary<string, WorkspacePanelCard> _cardsById = new();
    private readonly Dictionary<string, string> _panelTypeByCardId = new();

    private static readonly (string Type, string Icon, string Title)[] PanelTypes =
    {
        ("QuickCommands", "⚡", "Quick Commands"),
        ("SongRequests", "🎵", "Song Requests"),
        ("LiveChat", "💬", "Live Chat"),
        ("DashboardStats", "📊", "Today's Stats"),
        ("NowPlaying", "🔴", "Live Overlay"),
        ("StreamInfo", "📝", "Stream Title"),
        ("QuickGif", "🖼", "Quick GIF Send"),
        ("ObsScenes", "🎬", "OBS Scene Switcher")
    };

    public WorkspaceStudioForm()
    {
        Text = "Wadevo - Workspace Studio";
        Size = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = WadevoTheme.Colors.Background;
        Icon = TryLoadAppIcon();

        Panel topBar = new()
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = WadevoTheme.Colors.BackgroundSoft
        };

        _addPanelButton.ButtonText = "+ Add Panel ▾";
        _addPanelButton.Location = new Point(12, 8);
        _addPanelButton.Size = new Size(160, 34);
        _addPanelButton.AccentColor = WadevoTheme.Colors.Success;
        _addPanelButton.ButtonClicked += (_, _) => ShowAddPanelDropdown();

        _alwaysOnTopButton.Location = new Point(182, 8);
        _alwaysOnTopButton.Size = new Size(170, 34);
        _alwaysOnTopButton.ButtonClicked += (_, _) => ToggleAlwaysOnTop();

        topBar.Controls.Add(_addPanelButton);
        topBar.Controls.Add(_alwaysOnTopButton);

        _canvas.Dock = DockStyle.Fill;
        _canvas.BackColor = WadevoTheme.Colors.Background;
        _canvas.AutoScroll = true;

        Controls.Add(_canvas);
        Controls.Add(topBar);

        Load += (_, _) => LoadLayout();
        FormClosing += (_, _) => SaveLayout();
    }

    private static Icon? TryLoadAppIcon()
    {
        try
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "Logos", "WadevoLogo.ico");

            return File.Exists(path) ? new Icon(path) : null;
        }
        catch
        {
            return null;
        }
    }

    private void LoadLayout()
    {
        _layout = _layoutStore.Load();

        RefreshAlwaysOnTopButton();

        foreach (WorkspacePanelInstance instance in _layout.Panels)
        {
            AddPanelCard(instance);
        }
    }

    private void SaveLayout()
    {
        _layout.Panels = _cardsById.Select(pair => new WorkspacePanelInstance
        {
            Id = pair.Key,
            PanelType = _panelTypeByCardId.TryGetValue(pair.Key, out string? type) ? type : "",
            X = pair.Value.Location.X,
            Y = pair.Value.Location.Y,
            Width = pair.Value.Width,
            Height = pair.Value.Height
        }).ToList();

        _layoutStore.Save(_layout);
    }

    private void ToggleAlwaysOnTop()
    {
        _layout.AlwaysOnTop = !_layout.AlwaysOnTop;
        RefreshAlwaysOnTopButton();
        _layoutStore.Save(_layout);
    }

    private void RefreshAlwaysOnTopButton()
    {
        TopMost = _layout.AlwaysOnTop;
        _alwaysOnTopButton.ButtonText = _layout.AlwaysOnTop ? "📌 Always on Top" : "📌 Normal Window";
        _alwaysOnTopButton.AccentColor = _layout.AlwaysOnTop ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.TextMuted;
    }

    private void ShowAddPanelDropdown()
    {
        Panel content = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WadevoTheme.Colors.Background,
            Padding = new Padding(12)
        };

        int y = 8;

        foreach ((string type, string icon, string title) in PanelTypes)
        {
            WadevoButton option = new()
            {
                ButtonText = $"{icon} {title}",
                Location = new Point(12, y),
                Size = new Size(220, 36),
                AccentColor = WadevoTheme.Colors.Accent
            };

            option.ButtonClicked += (_, _) =>
            {
                WorkspacePanelInstance instance = new()
                {
                    PanelType = type,
                    X = 20 + (_cardsById.Count * 24) % 300,
                    Y = 20 + (_cardsById.Count * 24) % 200
                };

                AddPanelCard(instance);
            };

            content.Controls.Add(option);

            y += 44;
        }

        WadevoDropdownPopup popup = new(content, 250, y + 20);
        popup.ShowBelow(_addPanelButton);
    }

    private void AddPanelCard(WorkspacePanelInstance instance)
    {
        (string type, string icon, string title) = PanelTypes.FirstOrDefault(p => p.Type == instance.PanelType);

        if (string.IsNullOrEmpty(type))
        {
            return;
        }

        WorkspacePanelCard card = new($"{icon} {title}")
        {
            PanelId = instance.Id,
            Location = new Point(instance.X, instance.Y),
            Size = new Size(instance.Width, instance.Height)
        };

        Control content = CreatePanelContent(type);
        card.ContentHost.Controls.Add(content);

        card.CloseRequested += (_, _) =>
        {
            _canvas.Controls.Remove(card);
            _cardsById.Remove(instance.Id);
            _panelTypeByCardId.Remove(instance.Id);
            card.Dispose();
        };

        _panelTypeByCardId[instance.Id] = type;
        _cardsById[instance.Id] = card;

        _canvas.Controls.Add(card);
        card.BringToFront();
    }

    private static Control CreatePanelContent(string type)
    {
        return type switch
        {
            "QuickCommands" => new QuickCommandsPanelControl(),
            "SongRequests" => new SongRequestsPanelControl(),
            "LiveChat" => new LiveChatFeedPanelControl(),
            "DashboardStats" => new DashboardStatsPanelControl(),
            "NowPlaying" => new NowPlayingPanelControl(),
            "StreamInfo" => new StreamInfoPanelControl(),
            "QuickGif" => new QuickGifPanelControl(),
            "ObsScenes" => new ObsScenesPanelControl(),
            _ => new Panel()
        };
    }
}
