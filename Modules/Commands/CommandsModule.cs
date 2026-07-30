namespace Wadevo.Modules.Commands;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Modules;
using Wadevo.Services;
using Wadevo.Modules.Commands.Builder;

public class CommandsModule : WadevoModule
{
    private readonly CommandService CommandService = WadevoCommandHub.CommandService;

    private readonly WadevoScrollablePanel _commandListPanel = new();
    private readonly Panel _stageViewport = new();
    private readonly FlowLayoutPanel _stageContentPanel = new WadevoDoubleBufferedFlowLayoutPanel();

    private readonly WadevoSearchBox _searchBox = new();

    private readonly HashSet<string> _activeTypeFilters = new();
    private bool _showDisabledOnly;

    private static readonly (string Type, string Icon)[] FilterableTypes =
    {
        ("Chat Message", "💬"),
        ("Alert", "🚨"),
        ("GIF / Image", "🖼"),
        ("Video Clip", "🎬"),
        ("Sound Effect", "🔊"),
        ("Multi Action", "🎉"),
        ("Change OBS Scene", "🎥")
    };

    private readonly Label _studioTitleLabel = new();

    private WadevoButton? _duplicateButton;
    private WadevoButton? _testButton;
    private WadevoButton? _deleteButton;

    private CommandModel? _selectedCommand;

    public CommandsModule()
    {
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(0);
        AutoScroll = false;
        Dock = DockStyle.Fill;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        WadevoGlassCard listCard = CreateCommandListCard();
        WadevoGlassCard studioCard = CreateStudioCard();

        listCard.Dock = DockStyle.Fill;
        studioCard.Dock = DockStyle.Fill;

        listCard.Margin = new Padding(0, 0, 16, 0);
        studioCard.Margin = new Padding(0);

        layout.Controls.Add(listCard, 0, 0);
        layout.Controls.Add(studioCard, 1, 0);

        Controls.Add(layout);

        RefreshCommandList();
        ShowEmptyStudio();
    }

    private WadevoGlassCard CreateCommandListCard()
    {
        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Purple
        };

        WadevoButton newButton = CreateButton("+ New Command", WadevoTheme.Colors.Accent);
        newButton.Location = new Point(18, 20);
        newButton.Size = new Size(196, 42);
        newButton.ButtonClicked += (_, _) => StartNewCommand();

        Label title = new()
        {
            Text = "💬 Saved Commands",
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Purple,
            Location = new Point(18, 76),
            Size = new Size(206, 30),
            BackColor = Color.Transparent
        };

        _searchBox.Location = new Point(18, 112);
        _searchBox.Size = new Size(196, 38);
        _searchBox.PlaceholderText = "Search commands...";
        _searchBox.SearchTextChanged += (_, _) => RefreshCommandList();

        Panel filterRow = BuildFilterRow();
        filterRow.Location = new Point(18, 156);
        filterRow.Size = new Size(196, 38);

        _commandListPanel.Location = new Point(18, 202);
        _commandListPanel.Size = new Size(196, 400);
        _commandListPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _commandListPanel.BackColor = Color.Transparent;
        _commandListPanel.Content.Padding = new Padding(0, 0, 0, 12);

        card.Resize += (_, _) =>
        {
            int bottomPadding = 28;
            int availableHeight = card.ClientSize.Height - _commandListPanel.Top - bottomPadding;

            newButton.Width = Math.Max(120, card.ClientSize.Width - 36);
            _searchBox.Width = Math.Max(120, card.ClientSize.Width - 36);
            filterRow.Width = Math.Max(120, card.ClientSize.Width - 36);
            if (_filterTriggerButton is not null)
                _filterTriggerButton.Width = filterRow.Width;
            _commandListPanel.Height = Math.Max(120, availableHeight);
            _commandListPanel.Width = Math.Max(120, card.ClientSize.Width - 36);
        };

        card.Controls.Add(title);
        card.Controls.Add(newButton);
        card.Controls.Add(_searchBox);
        card.Controls.Add(filterRow);
        card.Controls.Add(_commandListPanel);

        return card;
    }

    private WadevoButton? _filterTriggerButton;

    private Panel BuildFilterRow()
    {
        Panel row = new()
        {
            BackColor = Color.Transparent
        };

        _filterTriggerButton = new WadevoButton
        {
            ButtonText = "Filter: All Types ▾",
            Location = new Point(0, 0),
            Size = new Size(196, 38),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        _filterTriggerButton.ButtonClicked += (_, _) => ShowFilterDropdown();

        row.Controls.Add(_filterTriggerButton);

        return row;
    }

    private void ShowFilterDropdown()
    {
        if (_filterTriggerButton is null)
        {
            return;
        }

        Panel content = new()
        {
            BackColor = WadevoTheme.Colors.Background
        };

        int y = 8;

        foreach ((string type, string icon) in FilterableTypes)
        {
            WadevoCheckBox checkBox = new()
            {
                Text = $"{icon}  {type}",
                Location = new Point(12, y),
                Size = new Size(220, 26),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.Text,
                Checked = _activeTypeFilters.Contains(type)
            };

            checkBox.CheckedChanged += (_, _) =>
            {
                if (checkBox.Checked)
                {
                    _activeTypeFilters.Add(type);
                }
                else
                {
                    _activeTypeFilters.Remove(type);
                }

                RefreshFilterTriggerLabel();
                RefreshCommandList();
            };

            content.Controls.Add(checkBox);
            y += 30;
        }

        WadevoCheckBox disabledOnlyCheckBox = new()
        {
            Text = "⏸  Disabled Only",
            Location = new Point(12, y + 6),
            Size = new Size(220, 26),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            Checked = _showDisabledOnly
        };

        disabledOnlyCheckBox.CheckedChanged += (_, _) =>
        {
            _showDisabledOnly = disabledOnlyCheckBox.Checked;

            RefreshFilterTriggerLabel();
            RefreshCommandList();
        };

        content.Controls.Add(disabledOnlyCheckBox);

        WadevoDropdownPopup popup = new(content, 244, y + 46);
        popup.ShowBelow(_filterTriggerButton);
    }

    private void RefreshFilterTriggerLabel()
    {
        if (_filterTriggerButton is null)
        {
            return;
        }

        int activeCount = _activeTypeFilters.Count + (_showDisabledOnly ? 1 : 0);

        _filterTriggerButton.ButtonText = activeCount == 0
            ? "Filter: All Types ▾"
            : $"Filter: {activeCount} active ▾";

        _filterTriggerButton.AccentColor = activeCount == 0
            ? WadevoTheme.Colors.TextMuted
            : WadevoTheme.Colors.Accent;
    }

    private WadevoGlassCard CreateStudioCard()
    {
        WadevoGlassCard card = new()
        {
            AccentColor = WadevoTheme.Colors.Accent
        };

        _studioTitleLabel.Text = "Command Studio";
        _studioTitleLabel.Font = WadevoTheme.Fonts.CardHeader;
        _studioTitleLabel.ForeColor = WadevoTheme.Colors.Accent;
        _studioTitleLabel.Location = new Point(28, 20);
        _studioTitleLabel.Size = new Size(520, 34);
        _studioTitleLabel.BackColor = Color.Transparent;

        _stageViewport.Location = new Point(30, 64);
        _stageViewport.Size = new Size(760, 636);
        _stageViewport.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _stageViewport.BackColor = Color.Transparent;

        _stageContentPanel.Location = new Point(0, 0);
        _stageContentPanel.Size = _stageViewport.Size;
        _stageContentPanel.BackColor = Color.Transparent;
        _stageContentPanel.FlowDirection = FlowDirection.TopDown;
        _stageContentPanel.WrapContents = false;
        _stageContentPanel.AutoScroll = true;
        _stageContentPanel.Padding = new Padding(0, 0, 12, 12);

        _stageViewport.Controls.Add(_stageContentPanel);

        _duplicateButton = CreateButton("📋 Duplicate", WadevoTheme.Colors.Cyan);
        WadevoButton duplicateButton = _duplicateButton;
        duplicateButton.Location = new Point(30, 600);
        duplicateButton.Size = new Size(140, 42);
        duplicateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        duplicateButton.Visible = false;
        duplicateButton.ButtonClicked += (_, _) => DuplicateCommand();

        _testButton = CreateButton("▶ Test", WadevoTheme.Colors.Success);
        WadevoButton testButton = _testButton;
        testButton.Location = new Point(180, 600);
        testButton.Size = new Size(120, 42);
        testButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        testButton.Visible = false;
        // Testing only works for an already-saved command - the wizard only hands back a
        // fully-built command at its very last step, so there's no "current partial
        // state" to test against while still mid-wizard on an earlier page.
        testButton.ButtonClicked += async (_, _) =>
        {
            if (_selectedCommand is null)
            {
                return;
            }

            if (_selectedCommand.CommandKind.Equals("Change OBS Scene", StringComparison.OrdinalIgnoreCase))
            {
                (bool success, string message) = await CommandExecutionService.ChangeObsSceneAsync(_selectedCommand.Response.Trim());

                WadevoMessageBox.Show(
                    FindForm(),
                    message,
                    success ? "Scene Switched" : "Couldn't Switch Scene");

                return;
            }

            WadevoCommandHub.ExecutionService.ExecuteDirectly(_selectedCommand);
        };

        _deleteButton = CreateButton("🗑 Delete", WadevoTheme.Colors.Error);
        WadevoButton deleteButton = _deleteButton;
        deleteButton.Location = new Point(310, 600);
        deleteButton.Size = new Size(120, 42);
        deleteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        deleteButton.Visible = false;
        deleteButton.ButtonClicked += (_, _) => DeleteCommand();

        void AdjustStageViewportSize()
        {
            int sidePadding = 60;
            int contentBottomPadding = 88;

            _stageViewport.Width = Math.Max(320, card.ClientSize.Width - sidePadding);
            _stageViewport.Height = Math.Max(160, card.ClientSize.Height - _stageViewport.Top - contentBottomPadding);
            _stageContentPanel.Size = _stageViewport.ClientSize;

            int buttonY = Math.Max(_stageViewport.Bottom + 16, card.ClientSize.Height - 58);

            duplicateButton.Top = buttonY;
            testButton.Top = buttonY;
            deleteButton.Top = buttonY;
        }

        AdjustStageViewportSize();

        card.Resize += (_, _) => AdjustStageViewportSize();
        card.HandleCreated += (_, _) => AdjustStageViewportSize();

        card.Controls.Add(_studioTitleLabel);
        card.Controls.Add(_stageViewport);
        card.Controls.Add(duplicateButton);
        card.Controls.Add(testButton);
        card.Controls.Add(deleteButton);

        return card;
    }

    private void StartNewCommand()
    {
        OpenCommandEditor(null);
    }

    private void OpenCommandEditor(CommandModel? commandToEdit)
    {
        _selectedCommand = commandToEdit;

        _studioTitleLabel.Text = commandToEdit is null
            ? "Create Command"
            : $"Edit \"{commandToEdit.Name}\"";

        if (_duplicateButton is not null)
            _duplicateButton.Visible = commandToEdit is not null;

        if (_testButton is not null)
            _testButton.Visible = commandToEdit is not null;

        if (_deleteButton is not null)
            _deleteButton.Visible = commandToEdit is not null;

        _stageContentPanel.SuspendLayout();

        foreach (Control control in _stageContentPanel.Controls.Cast<Control>().ToList())
        {
            _stageContentPanel.Controls.Remove(control);
            control.Dispose();
        }

        _stageContentPanel.ResumeLayout();

        _stageContentPanel.AutoScroll = false;

        EmbeddedCommandWizardControl wizard = new(commandToEdit);

        wizard.CommandBuilt += (_, wizardCommand) =>
        {
            CommandModel command = commandToEdit ?? CommandService.AddCommand(
                wizardCommand.Name,
                wizardCommand.Trigger,
                wizardCommand.CommandKind);

            command.Name = wizardCommand.Name;
            command.Trigger = wizardCommand.Trigger;
            command.TriggerMode = wizardCommand.TriggerMode;
            command.IntervalMinutes = wizardCommand.IntervalMinutes;
            command.LastFiredAt = wizardCommand.LastFiredAt;
            command.CommandKind = wizardCommand.CommandKind;
            command.Response = wizardCommand.Response;
            command.MediaFilePath = wizardCommand.MediaFilePath;
            command.Width = wizardCommand.Width;
            command.Height = wizardCommand.Height;
            command.DurationSeconds = wizardCommand.DurationSeconds;
            command.RequireExclamation = wizardCommand.RequireExclamation;
            command.IsEnabled = wizardCommand.IsEnabled;
            command.ShowInQuickPanel = wizardCommand.ShowInQuickPanel;

            CommandService.Save();

            RefreshCommandList();
            ShowEmptyStudio();
        };

        wizard.Cancelled += (_, _) => ShowEmptyStudio();

        Panel wizardHost = new()
        {
            Size = new Size(
                Math.Max(760, _stageContentPanel.ClientSize.Width - 4),
                Math.Max(430, _stageContentPanel.ClientSize.Height - 4)),
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };

        wizard.Dock = DockStyle.Fill;
        wizardHost.Controls.Add(wizard);

        _stageContentPanel.Resize += (_, _) =>
        {
            wizardHost.Size = new Size(
                Math.Max(760, _stageContentPanel.ClientSize.Width - 4),
                Math.Max(430, _stageContentPanel.ClientSize.Height - 4));
        };

        _stageContentPanel.Controls.Add(wizardHost);
        wizardHost.BringToFront();
    }

    private void RefreshCommandList()
    {
        _commandListPanel.Content.SuspendLayout();

        foreach (Control control in _commandListPanel.Content.Controls.Cast<Control>().ToList())
        {
            _commandListPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        string search = _searchBox.SearchText.Trim().ToLowerInvariant();

        IEnumerable<CommandModel> commands = CommandService.Commands;

        if (!string.IsNullOrWhiteSpace(search))
        {
            commands = commands.Where(command =>
                command.Name.ToLowerInvariant().Contains(search) ||
                command.Trigger.ToLowerInvariant().Contains(search) ||
                command.CommandKind.ToLowerInvariant().Contains(search));
        }

        if (_activeTypeFilters.Count > 0)
        {
            commands = commands.Where(command => _activeTypeFilters.Contains(command.CommandKind));
        }

        if (_showDisabledOnly)
        {
            commands = commands.Where(command => !command.IsEnabled);
        }

        commands = commands.OrderByDescending(command => command.CreatedAt);

        int itemWidth = Math.Max(120, _commandListPanel.ClientSize.Width - 20);

        foreach (CommandModel command in commands)
        {
            WadevoCommandListItem item = new()
            {
                CommandName = command.Name,
                Trigger = command.TriggerMode == "Timer"
                    ? $"⏱ every {command.IntervalMinutes} min"
                    : command.Trigger,
                CommandType = command.CommandKind,
                EnabledCommand = command.IsEnabled,
                Selected = command == _selectedCommand,
                Width = itemWidth,
                Margin = new Padding(0, 0, 0, 10)
            };

            item.ItemClicked += (_, _) =>
            {
                foreach (Control control in _commandListPanel.Content.Controls)
                {
                    if (control is WadevoCommandListItem listItem)
                        listItem.Selected = false;
                }

                item.Selected = true;

                OpenCommandEditor(command);
            };

            _commandListPanel.Content.Controls.Add(item);
        }

        _commandListPanel.Content.ResumeLayout();
        _commandListPanel.RefreshLayout();
    }

    private void ShowEmptyStudio()
    {
        _selectedCommand = null;

        _studioTitleLabel.Text = "Command Studio";

        if (_duplicateButton is not null)
            _duplicateButton.Visible = false;

        if (_testButton is not null)
            _testButton.Visible = false;

        if (_deleteButton is not null)
            _deleteButton.Visible = false;

        _stageContentPanel.SuspendLayout();

        foreach (Control control in _stageContentPanel.Controls.Cast<Control>().ToList())
        {
            _stageContentPanel.Controls.Remove(control);
            control.Dispose();
        }

        _stageContentPanel.ResumeLayout();

        _stageContentPanel.AutoScroll = true;

        Label empty = CreateBodyLabel(
            "Choose a command from the saved list, or click + New Command.\n\n" +
            "This is the Wadevo command builder.",
            520,
            120);

        _stageContentPanel.Controls.Add(empty);
    }

    private static Label CreateBodyLabel(string text, int width, int height)
    {
        return new Label
        {
            Text = text,
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            Size = new Size(width, height),
            Margin = new Padding(0, 0, 0, 12)
        };
    }

    private void DuplicateCommand()
    {
        if (_selectedCommand is null || !CommandService.Commands.Contains(_selectedCommand))
            return;

        CommandModel original = _selectedCommand;
        CommandModel duplicate = CommandService.DuplicateCommand(original);

        duplicate.Name = GetCopyName(original.Name);
        CommandService.Save();

        RefreshCommandList();
        OpenCommandEditor(duplicate);
    }

    private string GetCopyName(string originalName)
    {
        string baseName = string.IsNullOrWhiteSpace(originalName)
            ? "Untitled Command"
            : originalName.Trim();

        string copyName = $"{baseName} Copy";

        if (!CommandService.Commands.Any(command =>
                command.Name.Equals(copyName, StringComparison.OrdinalIgnoreCase)))
        {
            return copyName;
        }

        int copyNumber = 2;

        while (CommandService.Commands.Any(command =>
                   command.Name.Equals($"{copyName} {copyNumber}", StringComparison.OrdinalIgnoreCase)))
        {
            copyNumber++;
        }

        return $"{copyName} {copyNumber}";
    }

    private void DeleteCommand()
    {
        if (_selectedCommand is null || !CommandService.Commands.Contains(_selectedCommand))
            return;

        CommandService.RemoveCommand(_selectedCommand);

        RefreshCommandList();
        ShowEmptyStudio();
    }

    private static WadevoButton CreateButton(string text, Color color)
    {
        return new WadevoButton
        {
            ButtonText = text,
            AccentColor = color
        };
    }
}
