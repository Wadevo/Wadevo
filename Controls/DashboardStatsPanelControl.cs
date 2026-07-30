namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class DashboardStatsPanelControl : UserControl
{
    private readonly DashboardStatsService _statsService = WadevoDashboardHub.StatsService;
    private readonly Panel _grid = new();

    public DashboardStatsPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _statsService.StatsChanged += (_, _) =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(RefreshGrid));
            }
            else
            {
                RefreshGrid();
            }
        };

        _grid.Dock = DockStyle.Fill;
        _grid.BackColor = Color.Transparent;

        Controls.Add(_grid);

        RefreshGrid();
    }

    private void RefreshGrid()
    {
        _grid.SuspendLayout();

        foreach (Control control in _grid.Controls.Cast<Control>().ToList())
        {
            _grid.Controls.Remove(control);
            control.Dispose();
        }

        DashboardStatsModel stats = _statsService.GetStats();

        (string Label, int Value, Color Color)[] tiles =
        {
            ("Followers", stats.FollowCount, WadevoTheme.Colors.Accent),
            ("Subs", stats.SubscribeCount, WadevoTheme.Colors.Success),
            ("Gift Subs", stats.GiftSubCount, WadevoTheme.Colors.Warning),
            ("Raids", stats.RaidCount, WadevoTheme.Colors.Error),
            ("Commands", stats.CommandsExecutedCount, WadevoTheme.Colors.Cyan),
            ("Requests", stats.SongRequestsCount, WadevoTheme.Colors.Purple)
        };

        int tileWidth = 130;
        int tileHeight = 56;
        int gap = 8;
        int columns = 2;

        for (int i = 0; i < tiles.Length; i++)
        {
            int column = i % columns;
            int row = i / columns;

            Panel tile = new()
            {
                Location = new Point(column * (tileWidth + gap), row * (tileHeight + gap)),
                Size = new Size(tileWidth, tileHeight),
                BackColor = WadevoTheme.Colors.BackgroundSoft
            };

            Label valueLabel = new()
            {
                Text = tiles[i].Value.ToString(),
                Location = new Point(8, 4),
                Size = new Size(tileWidth - 16, 26),
                Font = WadevoTheme.Fonts.CardHeader,
                ForeColor = tiles[i].Color,
                BackColor = Color.Transparent
            };

            Label nameLabel = new()
            {
                Text = tiles[i].Label,
                Location = new Point(8, 32),
                Size = new Size(tileWidth - 16, 18),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            tile.Controls.Add(valueLabel);
            tile.Controls.Add(nameLabel);

            _grid.Controls.Add(tile);
        }

        _grid.ResumeLayout();
    }
}
