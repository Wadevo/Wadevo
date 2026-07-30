namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class SongRequestsPanelControl : UserControl
{
    private readonly SongRequestService _service = WadevoSongRequestHub.SongRequestService;
    private readonly WadevoScrollablePanel _listPanel = new();

    public SongRequestsPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _service.QueueChanged += (_, _) =>
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(RefreshList));
            }
            else
            {
                RefreshList();
            }
        };

        _listPanel.Dock = DockStyle.Fill;
        _listPanel.BackColor = Color.Transparent;

        Controls.Add(_listPanel);

        RefreshList();
    }

    private void RefreshList()
    {
        _listPanel.Content.SuspendLayout();

        foreach (Control control in _listPanel.Content.Controls.Cast<Control>().ToList())
        {
            _listPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        List<SongRequestModel> pending = _service.GetQueue()
            .Where(r => !r.IsPlayed)
            .OrderBy(r => r.RequestedAtUtc)
            .ToList();

        if (pending.Count == 0)
        {
            Label empty = new()
            {
                Text = "Queue is empty.",
                Size = new Size(260, 30),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            _listPanel.Content.Controls.Add(empty);
        }

        foreach (SongRequestModel request in pending)
        {
            Panel row = new()
            {
                Width = 280,
                Height = 52,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = WadevoTheme.Colors.BackgroundSoft
            };

            Label songLabel = new()
            {
                Text = request.SongText,
                Location = new Point(8, 6),
                Size = new Size(170, 20),
                Font = WadevoTheme.Fonts.Bold,
                ForeColor = WadevoTheme.Colors.Text,
                BackColor = Color.Transparent
            };

            Label requesterLabel = new()
            {
                Text = request.RequesterUsername,
                Location = new Point(8, 28),
                Size = new Size(170, 18),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            WadevoButton playedButton = new()
            {
                ButtonText = "✓",
                Location = new Point(188, 10),
                Size = new Size(40, 32),
                AccentColor = WadevoTheme.Colors.Success
            };

            playedButton.ButtonClicked += (_, _) => _service.MarkPlayed(request.Id);

            WadevoButton removeButton = new()
            {
                ButtonText = "✕",
                Location = new Point(232, 10),
                Size = new Size(40, 32),
                AccentColor = WadevoTheme.Colors.Error
            };

            removeButton.ButtonClicked += (_, _) => _service.Remove(request.Id);

            row.Controls.Add(songLabel);
            row.Controls.Add(requesterLabel);
            row.Controls.Add(playedButton);
            row.Controls.Add(removeButton);

            _listPanel.Content.Controls.Add(row);
        }

        _listPanel.Content.ResumeLayout();
        _listPanel.RefreshLayout();
    }
}
