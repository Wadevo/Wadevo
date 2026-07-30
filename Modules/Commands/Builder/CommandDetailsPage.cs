namespace Wadevo.Modules.Commands.Builder;

using Wadevo.Controls;
using Wadevo.Core;

public class CommandDetailsPage : CommandBuilderPage
{
    private readonly WadevoTextBox _nameTextBox = new();
    private readonly WadevoToggle _timerModeToggle = new();
    private readonly WadevoTextBox _triggerTextBox = new();
    private readonly WadevoTextBox _intervalTextBox = new();
    private readonly WadevoToggle _requirePrefixToggle = new();
    private readonly WadevoToggle _enableCommandToggle = new();
    private readonly WadevoToggle _pinToQuickPanelToggle = new();
    private readonly ToolTip _fieldTooltip = new();

    // These four controls occupy the same screen space and swap visibility based on the
    // timer toggle - only one "how does this fire" section makes sense to show at once.
    private readonly Label _triggerLabel;
    private readonly Label _triggerHelpLabel;
    private readonly Label _intervalLabel;
    private readonly Label _intervalHelpLabel;
    private readonly Label _requirePrefixLabel;

    public override string PageTitle => "Command Details";
    public override string PageSubtitle => "Name it, and choose how it fires.";

    public CommandDetailsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        Label nameLabel = CreateLabel("Command Name");
        nameLabel.Location = new Point(45, 35);

        _nameTextBox.Location = new Point(45, 65);
        _nameTextBox.Size = new Size(320, 44);
        _nameTextBox.PlaceholderText = "Example: Shoutout";

        _timerModeToggle.Location = new Point(45, 130);

        Label timerModeLabel = CreateLabel("Timer Instead of Chat Trigger");
        timerModeLabel.Location = new Point(135, 137);

        _fieldTooltip.SetToolTip(timerModeLabel,
            "Off: fires when someone types the chat trigger below. On: fires automatically " +
            "on a repeating interval, with no chat trigger involved at all - useful for " +
            "recurring reminders like 'don't forget to vote' or 'follow us on socials'.");

        // --- Chat Trigger mode fields ---

        _triggerLabel = CreateLabel("Chat Triggers");
        _triggerLabel.Location = new Point(45, 175);

        _triggerTextBox.Location = new Point(45, 205);
        _triggerTextBox.Size = new Size(500, 44);
        _triggerTextBox.PlaceholderText = "Example: !so, !shoutout, !raid";

        _triggerHelpLabel = new Label
        {
            Text = "Separate multiple triggers with commas. Example: !so, !shoutout, !raid",
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            Location = new Point(47, 258),
            Size = new Size(650, 24),
            BackColor = Color.Transparent
        };

        // --- Timer mode fields (same space, shown instead) ---

        _intervalLabel = CreateLabel("Fire Automatically Every");
        _intervalLabel.Location = new Point(45, 175);

        _intervalTextBox.Location = new Point(45, 205);
        _intervalTextBox.Size = new Size(150, 44);
        _intervalTextBox.PlaceholderText = "30";

        Label minutesLabel = new()
        {
            Text = "minutes",
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            Location = new Point(205, 218),
            Size = new Size(100, 30),
            BackColor = Color.Transparent
        };

        _intervalHelpLabel = new Label
        {
            Text = "Fires on its own while this timer runs - it never needs to be typed in chat.",
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            Location = new Point(47, 258),
            Size = new Size(650, 24),
            BackColor = Color.Transparent
        };

        _timerModeToggle.IsOnChanged += (_, _) => ApplyTriggerModeVisibility();

        // --- Shared fields below ---

        _requirePrefixToggle.Location = new Point(45, 310);

        _requirePrefixLabel = CreateLabel("Require ! Prefix");
        _requirePrefixLabel.Location = new Point(135, 317);

        _enableCommandToggle.Location = new Point(380, 310);

        Label enableLabel = CreateLabel("Enable Command");
        enableLabel.Location = new Point(470, 317);

        _pinToQuickPanelToggle.Location = new Point(380, 345);

        Label pinToQuickPanelLabel = CreateLabel("Pin to Quick Commands (Workspace Studio)");
        pinToQuickPanelLabel.Location = new Point(470, 352);
        pinToQuickPanelLabel.Size = new Size(260, 20);

        Controls.Add(nameLabel);
        Controls.Add(_nameTextBox);
        Controls.Add(_timerModeToggle);
        Controls.Add(timerModeLabel);
        Controls.Add(_triggerLabel);
        Controls.Add(_triggerTextBox);
        Controls.Add(_triggerHelpLabel);
        Controls.Add(_intervalLabel);
        Controls.Add(_intervalTextBox);
        Controls.Add(minutesLabel);
        Controls.Add(_intervalHelpLabel);
        Controls.Add(_requirePrefixToggle);
        Controls.Add(_requirePrefixLabel);
        Controls.Add(_enableCommandToggle);
        Controls.Add(enableLabel);
        Controls.Add(_pinToQuickPanelToggle);
        Controls.Add(pinToQuickPanelLabel);

        ApplyTriggerModeVisibility();
    }

    private void ApplyTriggerModeVisibility()
    {
        bool isTimer = _timerModeToggle.IsOn;

        _triggerLabel.Visible = !isTimer;
        _triggerTextBox.Visible = !isTimer;
        _triggerHelpLabel.Visible = !isTimer;

        _intervalLabel.Visible = isTimer;
        _intervalTextBox.Visible = isTimer;
        _intervalHelpLabel.Visible = isTimer;

        // A chat prefix is meaningless for something that fires on a timer rather than
        // from something someone typed in chat.
        _requirePrefixToggle.Visible = !isTimer;
        _requirePrefixLabel.Visible = !isTimer;
    }

    public override void LoadFromState(BuilderState state)
    {
        _nameTextBox.TextValue = state.CommandName;
        _timerModeToggle.IsOn = state.TriggerMode == "Timer";
        _triggerTextBox.TextValue = state.ChatTriggers;
        _intervalTextBox.TextValue = state.IntervalMinutes;
        _requirePrefixToggle.IsOn = state.RequirePrefix;
        _enableCommandToggle.IsOn = state.EnableCommand;
        _pinToQuickPanelToggle.IsOn = state.ShowInQuickPanel;

        ApplyTriggerModeVisibility();
    }

    public override void SaveToState(BuilderState state)
    {
        state.CommandName = _nameTextBox.TextValue.Trim();
        state.TriggerMode = _timerModeToggle.IsOn ? "Timer" : "Chat Trigger";
        state.IntervalMinutes = _intervalTextBox.TextValue.Trim();
        state.RequirePrefix = _requirePrefixToggle.IsOn;
        state.EnableCommand = _enableCommandToggle.IsOn;
        state.ShowInQuickPanel = _pinToQuickPanelToggle.IsOn;

        string triggers = _triggerTextBox.TextValue.Trim();

        state.ChatTriggers = state.RequirePrefix
            ? triggers
            : string.Join(", ", triggers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(trigger => trigger.TrimStart('!')));
    }

    public override bool CanMoveNext()
    {
        if (string.IsNullOrWhiteSpace(_nameTextBox.TextValue))
        {
            WadevoMessageBox.Show(FindForm(), "Command name cannot be empty.", "Wadevo");
            return false;
        }

        if (_timerModeToggle.IsOn)
        {
            if (!int.TryParse(_intervalTextBox.TextValue.Trim(), out int minutes) || minutes < 1)
            {
                WadevoMessageBox.Show(FindForm(), "Enter how many minutes between each automatic fire (1 or more).", "Wadevo");
                return false;
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(_triggerTextBox.TextValue))
        {
            WadevoMessageBox.Show(FindForm(), "Chat trigger cannot be empty.", "Wadevo");
            return false;
        }

        return true;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            Size = new Size(220, 26),
            BackColor = Color.Transparent
        };
    }
}
