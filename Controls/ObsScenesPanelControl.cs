namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services.Obs;
using Wadevo.Services.Soundboard;

public sealed class ObsScenesPanelControl : UserControl
{
    private readonly ObsConnectionService _obs = ObsConnectionService.Shared;
    private readonly ObsSceneHotkeyService _hotkeys = ObsSceneHotkeyService.Shared;

    private readonly WadevoScrollablePanel _listPanel = new();
    private readonly Label _statusLabel = new();

    private string? _listeningForScene;
    private Button? _listeningButton;

    public ObsScenesPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 22;
        _statusLabel.Font = WadevoTheme.Fonts.Small;
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.Padding = new Padding(4, 2, 0, 0);

        _listPanel.Dock = DockStyle.Fill;
        _listPanel.BackColor = Color.Transparent;

        Controls.Add(_listPanel);
        Controls.Add(_statusLabel);

        _obs.StateChanged += (_, _) => SafeRefreshList();
        _hotkeys.BindingsChanged += (_, _) => SafeRefreshList();

        RefreshList();
    }

    private void SafeRefreshList()
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(RefreshList);
            return;
        }

        RefreshList();
    }

    private async void RefreshList()
    {
        if (!_obs.IsConnected)
        {
            _statusLabel.Text = "⚪ OBS not connected - see Connections.";
            ClearList();
            return;
        }

        _statusLabel.Text = _obs.IsStreaming ? "🔴 Live" : "🟢 Connected";

        try
        {
            IReadOnlyList<string> scenes = await _obs.GetSceneListAsync();
            PopulateList(scenes);
        }
        catch
        {
            _statusLabel.Text = "⚠ Couldn't load scenes.";
        }
    }

    private void ClearList()
    {
        _listPanel.Content.SuspendLayout();

        foreach (Control control in _listPanel.Content.Controls.Cast<Control>().ToList())
        {
            _listPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        _listPanel.Content.ResumeLayout();
        _listPanel.RefreshLayout();
    }

    private void PopulateList(IReadOnlyList<string> scenes)
    {
        _listPanel.Content.SuspendLayout();

        foreach (Control control in _listPanel.Content.Controls.Cast<Control>().ToList())
        {
            _listPanel.Content.Controls.Remove(control);
            control.Dispose();
        }

        if (scenes.Count == 0)
        {
            Label empty = new()
            {
                Text = "No scenes found.",
                AutoSize = false,
                Size = new Size(260, 40),
                Font = WadevoTheme.Fonts.Default,
                ForeColor = WadevoTheme.Colors.TextMuted,
                Margin = new Padding(8)
            };

            _listPanel.Content.Controls.Add(empty);
            _listPanel.Content.ResumeLayout();
            _listPanel.RefreshLayout();
            return;
        }

        IReadOnlyList<ObsSceneHotkeyModel> bindings = _hotkeys.GetBindings();

        foreach (string sceneName in scenes)
        {
            _listPanel.Content.Controls.Add(BuildSceneRow(sceneName, bindings));
        }

        _listPanel.Content.ResumeLayout();
        _listPanel.RefreshLayout();
    }

    private Control BuildSceneRow(string sceneName, IReadOnlyList<ObsSceneHotkeyModel> bindings)
    {
        bool isCurrent = sceneName == _obs.CurrentSceneName;

        ObsSceneHotkeyModel? binding = bindings.FirstOrDefault(b => b.SceneName == sceneName);
        string hotkeyText = binding is { HasHotkey: true }
            ? HotkeyFormatting.DisplayText(binding.HotkeyModifiers, binding.HotkeyKey)
            : "No hotkey set";

        Panel row = new()
        {
            Size = new Size(260, 66),
            Margin = new Padding(4),
            BackColor = isCurrent ? WadevoTheme.Colors.BackgroundSoft : Color.Transparent
        };

        Label nameLabel = new()
        {
            Text = (isCurrent ? "🟢 " : "") + sceneName,
            Location = new Point(8, 6),
            Size = new Size(244, 22),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = isCurrent ? WadevoTheme.Colors.Success : WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        WadevoButton switchButton = new()
        {
            ButtonText = "Switch",
            Location = new Point(8, 30),
            Size = new Size(80, 30),
            AccentColor = WadevoTheme.Colors.Accent
        };
        switchButton.ButtonClicked += async (_, _) =>
        {
            try
            {
                await _obs.SetCurrentSceneAsync(sceneName);
            }
            catch
            {
                _statusLabel.Text = "⚠ Couldn't switch scenes.";
            }
        };

        Button hotkeyButton = new()
        {
            Text = hotkeyText,
            Location = new Point(94, 30),
            Size = new Size(158, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = WadevoTheme.Colors.Card,
            ForeColor = WadevoTheme.Colors.TextMuted,
            Font = WadevoTheme.Fonts.Small
        };
        hotkeyButton.FlatAppearance.BorderColor = WadevoTheme.Colors.Cyan;
        hotkeyButton.Click += (_, _) => BeginHotkeyCapture(sceneName, hotkeyButton);

        row.Controls.Add(nameLabel);
        row.Controls.Add(switchButton);
        row.Controls.Add(hotkeyButton);

        return row;
    }

    private void BeginHotkeyCapture(string sceneName, Button button)
    {
        if (_listeningButton is not null)
        {
            _listeningButton.Text = "No hotkey set";
        }

        _listeningForScene = sceneName;
        _listeningButton = button;
        button.Text = "Press a key... (Esc to cancel)";

        Form? host = FindForm();
        host?.Focus();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_listeningForScene is not null && _listeningButton is not null)
        {
            Keys baseKey = keyData & Keys.KeyCode;

            if (baseKey == Keys.Escape)
            {
                CancelHotkeyCapture();
                return true;
            }

            bool isModifierOnly =
                baseKey is Keys.ControlKey or Keys.ShiftKey or Keys.Menu
                    or Keys.LWin or Keys.RWin or Keys.None;

            if (isModifierOnly)
            {
                return true;
            }

            Keys modifierFlags = keyData & Keys.Modifiers;
            GlobalHotkeyService.HotkeyModifiers modifiers = GlobalHotkeyService.HotkeyModifiers.None;

            if (modifierFlags.HasFlag(Keys.Control)) modifiers |= GlobalHotkeyService.HotkeyModifiers.Control;
            if (modifierFlags.HasFlag(Keys.Alt)) modifiers |= GlobalHotkeyService.HotkeyModifiers.Alt;
            if (modifierFlags.HasFlag(Keys.Shift)) modifiers |= GlobalHotkeyService.HotkeyModifiers.Shift;

            CompleteHotkeyCapture(modifiers, baseKey);
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void CompleteHotkeyCapture(GlobalHotkeyService.HotkeyModifiers modifiers, Keys key)
    {
        string? sceneName = _listeningForScene;
        Button? button = _listeningButton;

        _listeningForScene = null;
        _listeningButton = null;

        if (sceneName is null || button is null)
        {
            return;
        }

        bool registered = _hotkeys.SetHotkey(sceneName, modifiers, key);

        if (!registered)
        {
            _statusLabel.Text = "⚠ That combo is already in use - try a different key.";
            button.Text = "No hotkey set";
            return;
        }

        button.Text = HotkeyFormatting.DisplayText(
            HotkeyFormatting.FormatModifiers(modifiers),
            key.ToString());
    }

    private void CancelHotkeyCapture()
    {
        if (_listeningButton is not null)
        {
            string sceneName = _listeningForScene ?? "";
            ObsSceneHotkeyModel? existing = _hotkeys.GetBindings().FirstOrDefault(b => b.SceneName == sceneName);

            _listeningButton.Text = existing is { HasHotkey: true }
                ? HotkeyFormatting.DisplayText(existing.HotkeyModifiers, existing.HotkeyKey)
                : "No hotkey set";
        }

        _listeningForScene = null;
        _listeningButton = null;
    }
}
