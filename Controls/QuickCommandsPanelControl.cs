namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;

public sealed class QuickCommandsPanelControl : UserControl
{
    private readonly WadevoScrollablePanel _listPanel = new();

    public QuickCommandsPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

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

        IReadOnlyList<CommandModel> allEnabled = WadevoCommandHub.CommandService.Commands
            .Where(c => c.IsEnabled)
            .ToList();

        IReadOnlyList<CommandModel> pinned = allEnabled
            .Where(c => c.ShowInQuickPanel)
            .ToList();

        // Showing only pinned commands once any exist gives real control over what
        // appears here - falling back to "all enabled" otherwise means this panel isn't
        // just empty by default for anyone who hasn't found the pin option yet.
        IReadOnlyList<CommandModel> commands = (pinned.Count > 0 ? pinned : allEnabled)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (commands.Count == 0)
        {
            Label empty = new()
            {
                Text = "No enabled commands yet.",
                Size = new Size(260, 30),
                Font = WadevoTheme.Fonts.Small,
                ForeColor = WadevoTheme.Colors.TextMuted,
                BackColor = Color.Transparent
            };

            _listPanel.Content.Controls.Add(empty);
        }

        foreach (CommandModel command in commands)
        {
            Panel row = new()
            {
                Width = 280,
                Height = 40,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = WadevoTheme.Colors.BackgroundSoft
            };

            Label nameLabel = new()
            {
                Text = command.Name,
                Location = new Point(8, 10),
                Size = new Size(170, 20),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.Text,
                BackColor = Color.Transparent
            };

            WadevoButton fireButton = new()
            {
                ButtonText = "Fire",
                Location = new Point(188, 4),
                Size = new Size(80, 32),
                AccentColor = WadevoTheme.Colors.Accent
            };

            fireButton.ButtonClicked += (_, _) =>
                WadevoCommandHub.ExecutionService.ExecuteTrigger(command.Trigger);

            row.Controls.Add(nameLabel);
            row.Controls.Add(fireButton);

            _listPanel.Content.Controls.Add(row);
        }

        _listPanel.Content.ResumeLayout();
        _listPanel.RefreshLayout();
    }
}
