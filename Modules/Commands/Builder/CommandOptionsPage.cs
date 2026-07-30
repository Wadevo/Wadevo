namespace Wadevo.Modules.Commands.Builder;

using Wadevo.Controls;
using Wadevo.Core;

public class CommandOptionsPage : CommandBuilderPage
{
    private readonly Label _outputLabel = new();
    private readonly Label _variablesHintLabel = new();
    private readonly TextBox _outputTextBox = new();
    private readonly WadevoButton _browseButton = new();
    private readonly WadevoButton _emoteButton = new();

    private readonly Label _multiActionHelpLabel = new();
    private readonly Label _addActionHeaderLabel = new();
    private readonly Label _listHeaderLabel = new();
    private readonly ListBox _actionListBox = new();
    private readonly WadevoComboBox _actionTypeComboBox = new();
    private readonly TextBox _actionValueTextBox = new();
    private readonly WadevoButton _actionBrowseButton = new();
    private readonly WadevoButton _addActionButton = new();
    private readonly WadevoButton _removeActionButton = new();
    private readonly WadevoButton _moveUpButton = new();
    private readonly WadevoButton _moveDownButton = new();

    private readonly Label _sizeLabel = new();
    private readonly Label _widthLabel = new();
    private readonly Label _heightLabel = new();
    private readonly Label _durationLabel = new();
    private readonly Label _widthUnitLabel = new();
    private readonly Label _heightUnitLabel = new();
    private readonly Label _durationUnitLabel = new();

    private readonly TextBox _widthTextBox = new();
    private readonly TextBox _heightTextBox = new();
    private readonly TextBox _durationTextBox = new();

    private string _commandType = "Chat Message";

    public override string PageTitle => "Command Options";
    public override string PageSubtitle => "Customize what this command does.";

    public CommandOptionsPage()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _outputLabel.Location = new Point(45, 35);
        _outputLabel.Size = new Size(350, 26);
        _outputLabel.Font = WadevoTheme.Fonts.Bold;
        _outputLabel.ForeColor = WadevoTheme.Colors.Text;
        _outputLabel.BackColor = Color.Transparent;

        _variablesHintLabel.Text = "Variables: {username}  {message}  {viewerCount}  {giftCount}  {voteAmount}";
        _variablesHintLabel.Location = new Point(45, 145);
        _variablesHintLabel.Size = new Size(560, 20);
        _variablesHintLabel.Font = WadevoTheme.Fonts.Small;
        _variablesHintLabel.ForeColor = WadevoTheme.Colors.Cyan;
        _variablesHintLabel.BackColor = Color.Transparent;

        _outputTextBox.Location = new Point(45, 70);
        _outputTextBox.Size = new Size(520, 72);
        _outputTextBox.Multiline = true;
        StyleTextBox(_outputTextBox);

        _browseButton.Text = "Browse...";
        _browseButton.Location = new Point(580, 70);
        _browseButton.Size = new Size(105, 36);
        _browseButton.Click += (_, _) => BrowseForFile();

        _emoteButton.ButtonText = "😀 Emote";
        _emoteButton.Location = new Point(580, 70);
        _emoteButton.Size = new Size(105, 36);
        _emoteButton.AccentColor = WadevoTheme.Colors.Accent;
        _emoteButton.ButtonClicked += (_, _) => EmotePickerPopup.ShowFor(_emoteButton, _outputTextBox);

        CreateMultiActionControls();
        CreateVisualOptionControls();

        Controls.Add(_outputLabel);
        Controls.Add(_variablesHintLabel);
        Controls.Add(_outputTextBox);
        Controls.Add(_browseButton);
        Controls.Add(_emoteButton);

        Controls.Add(_multiActionHelpLabel);
        Controls.Add(_actionListBox);
        Controls.Add(_actionTypeComboBox);
        Controls.Add(_actionValueTextBox);
        Controls.Add(_actionBrowseButton);
        Controls.Add(_addActionButton);
        Controls.Add(_removeActionButton);
        Controls.Add(_moveUpButton);
        Controls.Add(_moveDownButton);

        Controls.Add(_sizeLabel);
        Controls.Add(_widthLabel);
        Controls.Add(_widthTextBox);
        Controls.Add(_widthUnitLabel);

        Controls.Add(_heightLabel);
        Controls.Add(_heightTextBox);
        Controls.Add(_heightUnitLabel);

        Controls.Add(_durationLabel);
        Controls.Add(_durationTextBox);
        Controls.Add(_durationUnitLabel);
    }

    public override void LoadFromState(BuilderState state)
    {
        _commandType = state.CommandType;

        _outputTextBox.Text = state.Output;
        LoadActionsFromOutput(state.Output);

        _widthTextBox.Text = state.Width;
        _heightTextBox.Text = state.Height;
        _durationTextBox.Text = state.Duration;

        UpdateForCommandType();
    }

    public override void SaveToState(BuilderState state)
    {
        state.Output = _commandType == "Multi Action"
            ? BuildOutputFromActions()
            : _outputTextBox.Text.Trim();

        state.Width = _widthTextBox.Text.Trim();
        state.Height = _heightTextBox.Text.Trim();
        state.Duration = _durationTextBox.Text.Trim();
    }

    private void CreateMultiActionControls()
    {
        _multiActionHelpLabel.Text = "Build a sequence of actions that run together when this command is triggered. " +
                                      "Send Message and Show Alert support variables: {username} {message} {viewerCount} {giftCount} {voteAmount}";
        _multiActionHelpLabel.Location = new Point(45, 62);
        _multiActionHelpLabel.Size = new Size(650, 40);
        _multiActionHelpLabel.Font = WadevoTheme.Fonts.Default;
        _multiActionHelpLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _multiActionHelpLabel.BackColor = Color.Transparent;

        // The add-a-new-action row lives above the list rather than below it - this was
        // previously positioned below the list box at a fixed Y coordinate, and was
        // reported invisible twice despite the numbers looking correct on paper. Putting
        // it above removes any possibility of the list box's actual rendered bounds
        // overlapping and covering it, rather than continuing to guess at exact spacing.
        Label addActionHeaderLabel = _addActionHeaderLabel;
        addActionHeaderLabel.Text = "Add an action:";
        addActionHeaderLabel.Location = new Point(45, 106);
        addActionHeaderLabel.Size = new Size(200, 22);
        addActionHeaderLabel.Font = WadevoTheme.Fonts.Bold;
        addActionHeaderLabel.ForeColor = WadevoTheme.Colors.Text;
        addActionHeaderLabel.BackColor = Color.Transparent;

        _actionTypeComboBox.Location = new Point(45, 130);
        _actionTypeComboBox.Size = new Size(170, 34);
        _actionTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _actionTypeComboBox.FlatStyle = FlatStyle.Flat;
        _actionTypeComboBox.Font = WadevoTheme.Fonts.Default;
        _actionTypeComboBox.ForeColor = WadevoTheme.Colors.Text;
        _actionTypeComboBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _actionTypeComboBox.Items.AddRange(new object[]
        {
            "Show Alert",
            "Send Message",
            "Play GIF/Image",
            "Play Sound",
            "Switch OBS Scene",
            "Wait"
        });
        _actionTypeComboBox.SelectedIndex = 0;

        _actionValueTextBox.Location = new Point(225, 130);
        _actionValueTextBox.Size = new Size(245, 34);
        StyleTextBox(_actionValueTextBox);

        _actionBrowseButton.Text = "📁 Browse";
        _actionBrowseButton.Location = new Point(475, 128);
        _actionBrowseButton.Size = new Size(95, 38);
        _actionBrowseButton.AccentColor = WadevoTheme.Colors.Cyan;
        _actionBrowseButton.Click += (_, _) => BrowseForActionValue();

        _actionTypeComboBox.SelectedIndexChanged += (_, _) => UpdateActionValueInputForType();

        _addActionButton.Text = "+ Add to List";
        _addActionButton.Location = new Point(580, 128);
        _addActionButton.Size = new Size(140, 38);
        _addActionButton.AccentColor = WadevoTheme.Colors.Success;
        _addActionButton.Click += (_, _) => AddAction();

        Label listHeaderLabel = _listHeaderLabel;
        listHeaderLabel.Text = "Actions (runs top to bottom):";
        listHeaderLabel.Location = new Point(45, 174);
        listHeaderLabel.Size = new Size(300, 22);
        listHeaderLabel.Font = WadevoTheme.Fonts.Bold;
        listHeaderLabel.ForeColor = WadevoTheme.Colors.Text;
        listHeaderLabel.BackColor = Color.Transparent;

        _actionListBox.Location = new Point(45, 198);
        _actionListBox.Size = new Size(520, 160);
        _actionListBox.Font = WadevoTheme.Fonts.Medium;
        _actionListBox.ForeColor = WadevoTheme.Colors.Text;
        _actionListBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _actionListBox.BorderStyle = BorderStyle.FixedSingle;

        _removeActionButton.Text = "Remove";
        _removeActionButton.Location = new Point(580, 220);
        _removeActionButton.Size = new Size(105, 36);
        _removeActionButton.Click += (_, _) => RemoveSelectedAction();

        _moveUpButton.Text = "↑ Up";
        _moveUpButton.Location = new Point(580, 265);
        _moveUpButton.Size = new Size(105, 36);
        _moveUpButton.Click += (_, _) => MoveSelectedAction(-1);

        _moveDownButton.Text = "↓ Down";
        _moveDownButton.Location = new Point(580, 310);
        _moveDownButton.Size = new Size(105, 36);
        _moveDownButton.Click += (_, _) => MoveSelectedAction(1);

        UpdateActionValueInputForType();

        Controls.Add(addActionHeaderLabel);
        Controls.Add(listHeaderLabel);
    }

    private void CreateVisualOptionControls()
    {
        _sizeLabel.Text = "Overlay Size";
        _sizeLabel.Location = new Point(45, 180);
        StyleMainLabel(_sizeLabel);

        _widthLabel.Text = "Width";
        _widthLabel.Location = new Point(45, 215);
        StyleSmallLabel(_widthLabel);

        _widthTextBox.Location = new Point(45, 245);
        _widthTextBox.Size = new Size(100, 30);
        StyleTextBox(_widthTextBox);

        _widthUnitLabel.Text = "px";
        _widthUnitLabel.Location = new Point(152, 248);
        StyleUnitLabel(_widthUnitLabel);

        _heightLabel.Text = "Height";
        _heightLabel.Location = new Point(205, 215);
        StyleSmallLabel(_heightLabel);

        _heightTextBox.Location = new Point(205, 245);
        _heightTextBox.Size = new Size(100, 30);
        StyleTextBox(_heightTextBox);

        _heightUnitLabel.Text = "px";
        _heightUnitLabel.Location = new Point(312, 248);
        StyleUnitLabel(_heightUnitLabel);

        _durationLabel.Text = "Duration";
        _durationLabel.Location = new Point(365, 215);
        StyleSmallLabel(_durationLabel);

        _durationTextBox.Location = new Point(365, 245);
        _durationTextBox.Size = new Size(100, 30);
        StyleTextBox(_durationTextBox);

        _durationUnitLabel.Text = "sec";
        _durationUnitLabel.Location = new Point(472, 248);
        StyleUnitLabel(_durationUnitLabel);
    }

    private void UpdateForCommandType()
    {
        bool isMedia =
            _commandType == "GIF / Image" ||
            _commandType == "Video Clip" ||
            _commandType == "Sound Effect";

        bool isMultiAction = _commandType == "Multi Action";
        bool isSceneSelection = _commandType == "Change OBS Scene";
        bool isChatMessage = _commandType == "Chat Message";

        _outputLabel.Text = _commandType switch
        {
            "Chat Message" => "Message to Send",
            "GIF / Image" => "Media File",
            "Video Clip" => "Video File",
            "Sound Effect" => "Sound File",
            "Multi Action" => "Actions",
            "Alert" => "Alert Message",
            "Change OBS Scene" => "OBS Scene Name (must match exactly)",
            _ => "Command Output"
        };

        _outputTextBox.Visible = !isMultiAction;
        _browseButton.Visible = isMedia;
        _emoteButton.Visible = !isMultiAction && !isMedia && !isSceneSelection && !isChatMessage;
        _variablesHintLabel.Visible = !isMultiAction && !isMedia && !isSceneSelection;

        _outputTextBox.Multiline = !isMedia;
        _outputTextBox.Height = isMedia ? 30 : 72;

        _multiActionHelpLabel.Visible = isMultiAction;
        _actionListBox.Visible = isMultiAction;
        _actionTypeComboBox.Visible = isMultiAction;
        _actionValueTextBox.Visible = isMultiAction;
        _addActionButton.Visible = isMultiAction;
        _removeActionButton.Visible = isMultiAction;
        _moveUpButton.Visible = isMultiAction;
        _moveDownButton.Visible = isMultiAction;
        _addActionHeaderLabel.Visible = isMultiAction;
        _listHeaderLabel.Visible = isMultiAction;

        bool showVisualOptions =
            _commandType == "GIF / Image" ||
            _commandType == "Video Clip";

        _sizeLabel.Visible = showVisualOptions;
        _widthLabel.Visible = showVisualOptions;
        _widthTextBox.Visible = showVisualOptions;
        _widthUnitLabel.Visible = showVisualOptions;

        _heightLabel.Visible = showVisualOptions;
        _heightTextBox.Visible = showVisualOptions;
        _heightUnitLabel.Visible = showVisualOptions;

        _durationLabel.Visible = showVisualOptions;
        _durationTextBox.Visible = showVisualOptions;
        _durationUnitLabel.Visible = showVisualOptions;
    }

    private void UpdateActionValueInputForType()
    {
        string actionType = _actionTypeComboBox.Text.Trim();

        bool needsFileBrowse = actionType is "Play GIF/Image" or "Play Sound";

        _actionBrowseButton.Visible = needsFileBrowse;

        _actionValueTextBox.PlaceholderText = actionType switch
        {
            "Play GIF/Image" => "File path (or use Browse)",
            "Play Sound" => "File path (or use Browse)",
            "Show Alert" => "Alert message",
            "Send Message" => "Chat message text",
            "Switch OBS Scene" => "OBS scene name (must match exactly)",
            "Wait" => "Seconds to wait",
            _ => ""
        };
    }

    private void BrowseForActionValue()
    {
        string actionType = _actionTypeComboBox.Text.Trim();

        using OpenFileDialog dialog = new()
        {
            Filter = actionType == "Play Sound"
                ? "Audio files (*.mp3;*.wav;*.ogg)|*.mp3;*.wav;*.ogg"
                : "Image or video files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.webm;*.mov)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.mp4;*.webm;*.mov",
            Title = actionType == "Play Sound" ? "Choose a sound file" : "Choose an image or video"
        };

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
        {
            _actionValueTextBox.Text = dialog.FileName;
        }
    }

    public override void OnHostResized()
    {
        // Deliberately explicit rather than using Anchor or the Resize event - an anchor's
        // distance-from-edge gets computed based on this page's size at construction time
        // (before the wizard ever resizes it), and Resize doesn't reliably fire either since
        // Dock=Fill can already size the page correctly before the wizard's own explicit
        // .Size assignment runs, meaning that assignment sets the same value and triggers
        // nothing. Called directly by the wizard instead, right after it sets this page's
        // size, which is unambiguous regardless of what WinForms' layout engine already did.
        const int bottomMargin = 12;
        int newHeight = ClientSize.Height - _actionListBox.Top - bottomMargin;

        _actionListBox.Height = Math.Max(120, newHeight);
    }

    private void AddAction()
    {
        string actionType = _actionTypeComboBox.Text.Trim();
        string actionValue = _actionValueTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(actionType))
            actionType = "Show Alert";

        string display = string.IsNullOrWhiteSpace(actionValue)
            ? actionType
            : $"{actionType}: {actionValue}";

        _actionListBox.Items.Add(display);
        _actionListBox.SelectedIndex = _actionListBox.Items.Count - 1;
        _actionValueTextBox.Clear();
        _actionValueTextBox.Focus();
    }

    private void RemoveSelectedAction()
    {
        int index = _actionListBox.SelectedIndex;

        if (index < 0)
            return;

        _actionListBox.Items.RemoveAt(index);

        if (_actionListBox.Items.Count == 0)
            return;

        _actionListBox.SelectedIndex = Math.Min(index, _actionListBox.Items.Count - 1);
    }

    private void MoveSelectedAction(int direction)
    {
        int index = _actionListBox.SelectedIndex;

        if (index < 0)
            return;

        int newIndex = index + direction;

        if (newIndex < 0 || newIndex >= _actionListBox.Items.Count)
            return;

        object item = _actionListBox.Items[index];

        _actionListBox.Items.RemoveAt(index);
        _actionListBox.Items.Insert(newIndex, item);
        _actionListBox.SelectedIndex = newIndex;
    }

    private void LoadActionsFromOutput(string output)
    {
        _actionListBox.Items.Clear();

        if (string.IsNullOrWhiteSpace(output))
            return;

        string[] lines = output
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string line in lines)
            _actionListBox.Items.Add(line);
    }

    private string BuildOutputFromActions()
    {
        List<string> actions = new();

        foreach (object item in _actionListBox.Items)
        {
            string? text = item.ToString();

            if (!string.IsNullOrWhiteSpace(text))
                actions.Add(text.Trim());
        }

        return string.Join(Environment.NewLine, actions);
    }

    private void BrowseForFile()
    {
        using OpenFileDialog dialog = new();

        dialog.Title = "Choose File";
        dialog.Filter = _commandType switch
        {
            "GIF / Image" => "Image Files|*.gif;*.png;*.jpg;*.jpeg;*.webp|All Files|*.*",
            "Video Clip" => "Video Files|*.mp4;*.mov;*.avi;*.mkv;*.webm|All Files|*.*",
            "Sound Effect" => "Audio Files|*.mp3;*.wav;*.ogg;*.flac|All Files|*.*",
            _ => "All Files|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _outputTextBox.Text = dialog.FileName;
    }

    private static void StyleMainLabel(Label label)
    {
        label.Font = WadevoTheme.Fonts.Bold;
        label.ForeColor = WadevoTheme.Colors.Text;
        label.Size = new Size(220, 26);
        label.BackColor = Color.Transparent;
    }

    private static void StyleSmallLabel(Label label)
    {
        label.Font = WadevoTheme.Fonts.Medium;
        label.ForeColor = WadevoTheme.Colors.TextMuted;
        label.Size = new Size(130, 26);
        label.BackColor = Color.Transparent;
    }

    private static void StyleUnitLabel(Label label)
    {
        label.Font = WadevoTheme.Fonts.Medium;
        label.ForeColor = WadevoTheme.Colors.TextMuted;
        label.Size = new Size(45, 24);
        label.BackColor = Color.Transparent;
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.Font = WadevoTheme.Fonts.Medium;
        textBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        textBox.ForeColor = WadevoTheme.Colors.Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }
}
