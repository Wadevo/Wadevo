namespace Wadevo.Modules;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class ConnectionsHubModule : WadevoModule
{
    public override string ModuleName => "Connections";
    public override string ModuleDescription => "Connect once. Every Wadevo feature can use it.";

    public event Action<string>? OpenRequested;

    private readonly WadevoScrollablePanel _scrollPanel = new();

    public ConnectionsHubModule()
    {
        Padding = new Padding(0);

        _scrollPanel.Dock = DockStyle.Fill;
        _scrollPanel.Padding = new Padding(WadevoTheme.Sizes.SpaceXL, WadevoTheme.Sizes.SpaceM, WadevoTheme.Sizes.SpaceXL, WadevoTheme.Sizes.SpaceM);
        _scrollPanel.BackColor = Color.Transparent;

        Controls.Add(_scrollPanel);

        Build();
    }

    public void RefreshConnections()
    {
        _scrollPanel.Content.Controls.Clear();
        Build();
    }

    private void Build()
    {
        List<ConnectionInfoModel> connections = ConnectionsHubService.GetConnections();

        Label streamingHeader = BuildCategoryHeader("STREAMING");
        FlowLayoutPanel streamingRow = BuildCategoryRow(connections, ConnectionCategory.Streaming);
        Panel spacer1 = new() { Dock = DockStyle.Top, Height = WadevoTheme.Sizes.SpaceM, BackColor = Color.Transparent };

        Label musicHeader = BuildCategoryHeader("MUSIC");
        FlowLayoutPanel musicRow = BuildCategoryRow(connections, ConnectionCategory.Music);
        Panel spacer2 = new() { Dock = DockStyle.Top, Height = WadevoTheme.Sizes.SpaceM, BackColor = Color.Transparent };

        Label softwareHeader = BuildCategoryHeader("SOFTWARE");
        FlowLayoutPanel softwareRow = BuildCategoryRow(connections, ConnectionCategory.Software);

        _scrollPanel.Content.Controls.Add(streamingHeader);
        _scrollPanel.Content.Controls.Add(streamingRow);
        _scrollPanel.Content.Controls.Add(spacer1);
        _scrollPanel.Content.Controls.Add(musicHeader);
        _scrollPanel.Content.Controls.Add(musicRow);
        _scrollPanel.Content.Controls.Add(spacer2);
        _scrollPanel.Content.Controls.Add(softwareHeader);
        _scrollPanel.Content.Controls.Add(softwareRow);

        _scrollPanel.RefreshLayout();
    }

    private static Label BuildCategoryHeader(string text)
    {
        return new Label
        {
            Text = text,
            Width = 700,
            Height = 26,
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, WadevoTheme.Sizes.SpaceXS)
        };
    }

    private FlowLayoutPanel BuildCategoryRow(List<ConnectionInfoModel> connections, ConnectionCategory category)
    {
        FlowLayoutPanel row = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        foreach (ConnectionInfoModel connection in connections.Where(c => c.Category == category))
        {
            row.Controls.Add(BuildConnectionCard(connection));
        }

        return row;
    }

    private WadevoGlassCard BuildConnectionCard(ConnectionInfoModel connection)
    {
        Color accentColor = connection.State switch
        {
            ConnectionState.Connected => WadevoTheme.Colors.Success,
            ConnectionState.Warning => WadevoTheme.Colors.Warning,
            ConnectionState.ComingSoon => WadevoTheme.Colors.TextMuted,
            _ => WadevoTheme.Colors.Border
        };

        WadevoGlassCard card = new()
        {
            Size = new Size(270, 180),
            Margin = new Padding(0, 0, WadevoTheme.Sizes.SpaceS, WadevoTheme.Sizes.SpaceS),
            AccentColor = accentColor,
            // Glow is reserved for what's actually live, so it draws the eye to
            // connections that are working rather than lighting up the whole grid.
            ShowGlow = connection.State == ConnectionState.Connected,
            Padding = new Padding(0)
        };

        Label glyphLabel = new()
        {
            Text = connection.Glyph,
            Location = new Point(16, 14),
            Size = new Size(36, 32),
            Font = WadevoTheme.Fonts.CardHeader,
            BackColor = Color.Transparent
        };

        Label nameLabel = new()
        {
            Text = connection.Name,
            Location = new Point(56, 16),
            Size = new Size(190, 26),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        Label statusLabel = new()
        {
            Text = "● " + connection.StatusText,
            Location = new Point(16, 54),
            Size = new Size(240, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = accentColor,
            BackColor = Color.Transparent
        };

        Label descriptionLabel = new()
        {
            Text = connection.Description,
            Location = new Point(16, 78),
            Size = new Size(240, 46),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        WadevoButton actionButton = new()
        {
            ButtonText = connection.State == ConnectionState.ComingSoon ? "Coming Soon" : "Open",
            Location = new Point(16, 134),
            Size = new Size(238, 30),
            Enabled = connection.CanOpen,
            AccentColor = accentColor
        };

        if (connection.CanOpen)
        {
            actionButton.ButtonClicked += (_, _) => OpenRequested?.Invoke(connection.Name);
        }

        card.Controls.Add(glyphLabel);
        card.Controls.Add(nameLabel);
        card.Controls.Add(statusLabel);
        card.Controls.Add(descriptionLabel);
        card.Controls.Add(actionButton);

        return card;
    }
}
