namespace Wadevo.Modules.Commands.Builder;

using Wadevo.Controls;
using Wadevo.Core;

public class CommandPreviewPage : CommandBuilderPage
{
    private readonly WadevoGlassCard _summaryCard = new();
    private readonly Label _summaryTitle = new();
    private readonly Label _typeBadge = new();
    private readonly Label _nameLabel = new();
    private readonly Label _triggerLabel = new();
    private readonly Label _outputLabel = new();
    private readonly Label _visualLabel = new();
    private readonly WadevoButton _testButton = new();

    private BuilderState _currentState = new();

    public override string PageTitle => "Preview & Build";
    public override string PageSubtitle => "Review your command before building it.";

    public CommandPreviewPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _summaryCard.Location = new Point(40, 20);
        _summaryCard.Size = new Size(700, 245);
        _summaryCard.AccentColor = WadevoTheme.Colors.Cyan;

        _summaryTitle.Text = "Command Preview";
        _summaryTitle.Location = new Point(24, 18);
        _summaryTitle.Size = new Size(300, 32);
        _summaryTitle.Font = WadevoTheme.Fonts.CardHeader;
        _summaryTitle.ForeColor = WadevoTheme.Colors.Cyan;
        _summaryTitle.BackColor = Color.Transparent;

        _typeBadge.Location = new Point(540, 22);
        _typeBadge.Size = new Size(130, 26);
        _typeBadge.Font = WadevoTheme.Fonts.Bold;
        _typeBadge.ForeColor = Color.Black;
        _typeBadge.BackColor = WadevoTheme.Colors.Cyan;
        _typeBadge.TextAlign = ContentAlignment.MiddleCenter;

        _nameLabel.Location = new Point(28, 65);
        _nameLabel.Size = new Size(640, 32);
        StyleValueLabel(_nameLabel, WadevoTheme.Fonts.CardHeader, WadevoTheme.Colors.Text);

        _triggerLabel.Location = new Point(30, 105);
        _triggerLabel.Size = new Size(640, 26);
        StyleValueLabel(_triggerLabel, WadevoTheme.Fonts.Bold, WadevoTheme.Colors.Cyan);

        _outputLabel.Location = new Point(30, 143);
        _outputLabel.Size = new Size(640, 42);
        StyleValueLabel(_outputLabel, WadevoTheme.Fonts.Medium, WadevoTheme.Colors.TextMuted);

        _visualLabel.Location = new Point(30, 195);
        _visualLabel.Size = new Size(640, 28);
        StyleValueLabel(_visualLabel, WadevoTheme.Fonts.Medium, WadevoTheme.Colors.Accent);

        _summaryCard.Controls.Add(_summaryTitle);
        _summaryCard.Controls.Add(_typeBadge);
        _summaryCard.Controls.Add(_nameLabel);
        _summaryCard.Controls.Add(_triggerLabel);
        _summaryCard.Controls.Add(_outputLabel);
        _summaryCard.Controls.Add(_visualLabel);

        _testButton.ButtonText = "▶ Test Command";
        _testButton.AccentColor = WadevoTheme.Colors.Cyan;
        _testButton.Location = new Point(40, 275);
        _testButton.Size = new Size(180, 42);
        _testButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _testButton.ButtonClicked += (_, _) => ShowTestPreview();

        Controls.Add(_summaryCard);
        Controls.Add(_testButton);
    }

    public override void LoadFromState(BuilderState state)
    {
        _currentState = state;

        string commandName = string.IsNullOrWhiteSpace(state.CommandName)
            ? "Untitled Command"
            : state.CommandName.Trim();

        string triggers = string.IsNullOrWhiteSpace(state.ChatTriggers)
            ? "No triggers set"
            : state.ChatTriggers.Trim();

        string output = string.IsNullOrWhiteSpace(state.Output)
            ? "No output selected"
            : state.Output.Trim();

        _typeBadge.Text = GetShortTypeName(state.CommandType);
        _nameLabel.Text = $"{GetIcon(state.CommandType)}  {commandName}";
        _triggerLabel.Text = $"Trigger: {triggers}";
        _outputLabel.Text = $"Output: {output}";

        bool hasVisualOptions =
            state.CommandType == "GIF / Image" ||
            state.CommandType == "Video Clip";

        _visualLabel.Visible = hasVisualOptions;

        if (hasVisualOptions)
        {
            string width = string.IsNullOrWhiteSpace(state.Width) ? "Auto" : state.Width.Trim();
            string height = string.IsNullOrWhiteSpace(state.Height) ? "Auto" : state.Height.Trim();
            string duration = string.IsNullOrWhiteSpace(state.Duration) ? "Default" : state.Duration.Trim();

            _visualLabel.Text = $"Overlay: {width}px × {height}px   •   Duration: {duration} sec";
        }
    }

    private void ShowTestPreview()
    {
        try
        {
            using CommandOverlayPreviewForm preview = new(_currentState)
            {
                StartPosition = FormStartPosition.CenterScreen,
                ShowInTaskbar = true,
                TopMost = true
            };

            preview.ShowDialog();
        }
        catch (Exception ex)
        {
            WadevoMessageBox.Show(FindForm(), ex.Message, "Command Preview Error");
        }
    }

    private static void StyleValueLabel(Label label, Font font, Color color)
    {
        label.Font = font;
        label.ForeColor = color;
        label.BackColor = Color.Transparent;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static string GetIcon(string commandType)
    {
        return commandType switch
        {
            "Chat Message" => "💬",
            "Alert" => "🔔",
            "GIF / Image" => "🖼",
            "Video Clip" => "🎬",
            "Sound Effect" => "🔊",
            "Multi Action" => "🎉",
            "Change OBS Scene" => "🎥",
            _ => "⭐"
        };
    }

    private static string GetShortTypeName(string commandType)
    {
        return commandType switch
        {
            "Chat Message" => "CHAT",
            "Alert" => "ALERT",
            "GIF / Image" => "MEDIA",
            "Video Clip" => "VIDEO",
            "Sound Effect" => "SOUND",
            "Multi Action" => "MULTI",
            "Change OBS Scene" => "SCENE",
            _ => "COMMAND"
        };
    }
}