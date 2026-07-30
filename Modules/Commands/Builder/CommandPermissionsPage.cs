namespace Wadevo.Modules.Commands.Builder;

using Wadevo.Controls;
using Wadevo.Core;

public class CommandPermissionsPage : CommandBuilderPage
{
    private readonly WadevoTextBox _cooldownTextBox = new();
    private readonly WadevoComboBox _minimumRoleCombo = new();
    private readonly ToolTip _fieldTooltip = new();

    private readonly Label _cooldownLabel;
    private readonly Label _minimumRoleLabel;
    private readonly Label _roleHelpLabel;

    public override string PageTitle => "Permissions";
    public override string PageSubtitle => "Set a cooldown and decide who's allowed to use this command.";

    public CommandPermissionsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _cooldownLabel = CreateLabel("Cooldown (seconds) ⓘ");
        _cooldownLabel.Location = new Point(45, 40);
        _cooldownLabel.Cursor = Cursors.Help;

        _cooldownTextBox.Location = new Point(45, 72);
        _cooldownTextBox.Size = new Size(220, 44);
        _cooldownTextBox.PlaceholderText = "0 = no cooldown";

        _minimumRoleLabel = CreateLabel("Who Can Use This ⓘ");
        _minimumRoleLabel.Location = new Point(45, 150);
        _minimumRoleLabel.Cursor = Cursors.Help;

        _minimumRoleCombo.Location = new Point(45, 182);
        _minimumRoleCombo.Size = new Size(280, 30);
        _minimumRoleCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _minimumRoleCombo.Items.AddRange(new object[] { "Everyone", "Follower", "Subscriber", "Moderator", "Owner" });

        _fieldTooltip.SetToolTip(_cooldownLabel,
            "How long someone has to wait before this command can fire again. Set to 0 to allow " +
            "it every time - useful for stopping chat spam on commands people use a lot.");

        _fieldTooltip.SetToolTip(_minimumRoleLabel,
            "The lowest level someone needs to be to use this command. 'Everyone' means anyone in " +
            "chat. Moderators and you can always use any command, no matter what this is set to - " +
            "except 'Owner', which locks it to only you, not even your mods.");

        _roleHelpLabel = new Label
        {
            Text = "Moderators and the channel owner can always use any command - except 'Owner', which locks it to only you.",
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            Location = new Point(45, 224),
            Size = new Size(600, 24),
            BackColor = Color.Transparent
        };

        Controls.Add(_cooldownLabel);
        Controls.Add(_cooldownTextBox);
        Controls.Add(_minimumRoleLabel);
        Controls.Add(_minimumRoleCombo);
        Controls.Add(_roleHelpLabel);
    }

    public override void LoadFromState(BuilderState state)
    {
        _cooldownTextBox.TextValue = state.CooldownSeconds;

        int roleIndex = _minimumRoleCombo.Items.IndexOf(state.MinimumRole);
        _minimumRoleCombo.SelectedIndex = roleIndex >= 0 ? roleIndex : 0;
    }

    public override void SaveToState(BuilderState state)
    {
        state.CooldownSeconds = _cooldownTextBox.TextValue.Trim();
        state.MinimumRole = _minimumRoleCombo.SelectedItem?.ToString() ?? "Everyone";
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            Size = new Size(280, 26),
            BackColor = Color.Transparent
        };
    }
}
