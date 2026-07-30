namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services.Obs;

public sealed class ObsConnectionPopupForm : WadevoDialogForm
{
    private readonly ObsConnectionService _obs = ObsConnectionService.Shared;

    private readonly Label _statusLabel = new();
    private readonly Label _sceneLabel = new();
    private readonly TextBox _hostTextBox = new();
    private readonly TextBox _portTextBox = new();
    private readonly TextBox _passwordTextBox = new();
    private readonly WadevoCheckBox _autoConnectCheckBox = new();
    private readonly ListBox _sceneListBox = new();
    private readonly WadevoButton _connectButton = new();

    public ObsConnectionPopupForm() : base("🎬 OBS Connection")
    {
        Size = new Size(560, 640);
        ContentPanel.AutoScroll = true;

        _obs.StateChanged += (_, _) => SafeRefreshStatus();

        Label intro = new()
        {
            Text = "Setup: in OBS, go to Tools > WebSocket Server Settings. Make sure \"Enable " +
                   "WebSocket server\" is checked, then click Apply. If \"Enable Authentication\" " +
                   "is checked, copy that password below - if it's unchecked, leave the password " +
                   "field blank here. Then click Connect.",
            Location = new Point(24, 16),
            Size = new Size(500, 56),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _statusLabel.Text = "⚪ Not connected";
        _statusLabel.Location = new Point(24, 82);
        _statusLabel.Size = new Size(400, 26);
        _statusLabel.Font = WadevoTheme.Fonts.Bold;
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _statusLabel.BackColor = Color.Transparent;

        _sceneLabel.Text = "";
        _sceneLabel.Location = new Point(24, 110);
        _sceneLabel.Size = new Size(500, 22);
        _sceneLabel.Font = WadevoTheme.Fonts.Small;
        _sceneLabel.ForeColor = WadevoTheme.Colors.Cyan;
        _sceneLabel.BackColor = Color.Transparent;

        Label hostLabel = FieldLabel("Host", 24, 126);
        _hostTextBox.Location = new Point(24, 168);
        _hostTextBox.Size = new Size(220, 28);
        _hostTextBox.Text = _obs.Settings.Host;

        Label portLabel = FieldLabel("Port", 260, 126);
        _portTextBox.Location = new Point(260, 168);
        _portTextBox.Size = new Size(100, 28);
        _portTextBox.Text = _obs.Settings.Port.ToString();

        Label passwordLabel = FieldLabel("WebSocket password", 24, 184);
        _passwordTextBox.Location = new Point(24, 226);
        _passwordTextBox.Size = new Size(288, 28);
        _passwordTextBox.UseSystemPasswordChar = true;
        _passwordTextBox.Text = _obs.Settings.Password;

        WadevoButton showPasswordButton = new()
        {
            ButtonText = "👁 Show",
            Location = new Point(320, 224),
            Size = new Size(90, 32),
            AccentColor = WadevoTheme.Colors.TextMuted
        };
        showPasswordButton.ButtonClicked += (_, _) =>
        {
            _passwordTextBox.UseSystemPasswordChar = !_passwordTextBox.UseSystemPasswordChar;
            showPasswordButton.ButtonText = _passwordTextBox.UseSystemPasswordChar ? "👁 Show" : "🙈 Hide";
        };

        _autoConnectCheckBox.Text = "Connect automatically when Wadevo starts";
        _autoConnectCheckBox.Location = new Point(24, 264);
        _autoConnectCheckBox.Size = new Size(360, 24);
        _autoConnectCheckBox.Checked = _obs.Settings.AutoConnect;

        WadevoButton saveButton = new()
        {
            ButtonText = "Save Settings",
            Location = new Point(24, 298),
            Size = new Size(140, 36),
            AccentColor = WadevoTheme.Colors.Cyan
        };
        saveButton.ButtonClicked += (_, _) => SaveSettings();

        _connectButton.ButtonText = "Connect";
        _connectButton.Location = new Point(174, 298);
        _connectButton.Size = new Size(120, 36);
        _connectButton.AccentColor = WadevoTheme.Colors.Accent;
        _connectButton.ButtonClicked += ConnectButton_Click;

        WadevoButton disconnectButton = new()
        {
            ButtonText = "Disconnect",
            Location = new Point(304, 298),
            Size = new Size(120, 36),
            AccentColor = WadevoTheme.Colors.Error
        };
        disconnectButton.ButtonClicked += async (_, _) =>
        {
            await _obs.DisconnectAsync();
            RefreshStatus();
        };

        Label sceneListHeader = new()
        {
            Text = "Scenes",
            Location = new Point(24, 350),
            Size = new Size(200, 24),
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        _sceneListBox.Location = new Point(24, 378);
        _sceneListBox.Size = new Size(500, 180);
        _sceneListBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _sceneListBox.ForeColor = WadevoTheme.Colors.Text;
        _sceneListBox.BorderStyle = BorderStyle.FixedSingle;
        _sceneListBox.Font = WadevoTheme.Fonts.Default;

        WadevoButton refreshScenesButton = new()
        {
            ButtonText = "🔄 Refresh Scenes",
            Location = new Point(24, 566),
            Size = new Size(160, 36),
            AccentColor = WadevoTheme.Colors.Cyan
        };
        refreshScenesButton.ButtonClicked += (_, _) => _ = RefreshScenesAsync();

        WadevoButton switchSceneButton = new()
        {
            ButtonText = "Switch to Selected",
            Location = new Point(194, 566),
            Size = new Size(160, 36),
            AccentColor = WadevoTheme.Colors.Accent
        };
        switchSceneButton.ButtonClicked += (_, _) => _ = SwitchToSelectedSceneAsync();

        ContentPanel.Controls.Add(intro);
        ContentPanel.Controls.Add(_statusLabel);
        ContentPanel.Controls.Add(_sceneLabel);
        ContentPanel.Controls.Add(hostLabel);
        ContentPanel.Controls.Add(_hostTextBox);
        ContentPanel.Controls.Add(portLabel);
        ContentPanel.Controls.Add(_portTextBox);
        ContentPanel.Controls.Add(passwordLabel);
        ContentPanel.Controls.Add(_passwordTextBox);
        ContentPanel.Controls.Add(showPasswordButton);
        ContentPanel.Controls.Add(_autoConnectCheckBox);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(_connectButton);
        ContentPanel.Controls.Add(disconnectButton);
        ContentPanel.Controls.Add(sceneListHeader);
        ContentPanel.Controls.Add(_sceneListBox);
        ContentPanel.Controls.Add(refreshScenesButton);
        ContentPanel.Controls.Add(switchSceneButton);

        RefreshStatus();

        if (_obs.IsConnected)
        {
            _ = RefreshScenesAsync();
        }
    }

    private static Label FieldLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(220, 20),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };
    }

    private void SaveSettings()
    {
        if (!int.TryParse(_portTextBox.Text.Trim(), out int port))
        {
            port = 4455;
        }

        ObsConnectionSettings settings = new()
        {
            Host = string.IsNullOrWhiteSpace(_hostTextBox.Text) ? "localhost" : _hostTextBox.Text.Trim(),
            Port = port,
            Password = _passwordTextBox.Text,
            AutoConnect = _autoConnectCheckBox.Checked
        };

        _obs.UpdateSettings(settings);
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        SaveSettings();

        try
        {
            _connectButton.Enabled = false;
            await _obs.ConnectAsync();
            await RefreshScenesAsync();
        }
        catch
        {
            // Status label already reflects the failure via ObsConnectionService.StatusMessage.
        }
        finally
        {
            _connectButton.Enabled = true;
            RefreshStatus();
        }
    }

    private async Task RefreshScenesAsync()
    {
        if (!_obs.IsConnected)
        {
            return;
        }

        try
        {
            IReadOnlyList<string> scenes = await _obs.GetSceneListAsync();

            _sceneListBox.Items.Clear();

            foreach (string scene in scenes)
            {
                _sceneListBox.Items.Add(scene);
            }
        }
        catch
        {
            // Leave the list as-is on failure.
        }
    }

    private async Task SwitchToSelectedSceneAsync()
    {
        if (_sceneListBox.SelectedItem is not string sceneName)
        {
            return;
        }

        try
        {
            await _obs.SetCurrentSceneAsync(sceneName);
        }
        catch
        {
            // Status/scene label will simply not update if this fails.
        }
    }

    private void SafeRefreshStatus()
    {
        if (InvokeRequired)
        {
            BeginInvoke(RefreshStatus);
            return;
        }

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (_obs.IsConnected)
        {
            _statusLabel.Text = _obs.IsStreaming ? "🔴 Connected - Streaming Live" : "🟢 Connected";
            _statusLabel.ForeColor = _obs.IsStreaming ? WadevoTheme.Colors.Error : WadevoTheme.Colors.Success;
            _sceneLabel.Text = string.IsNullOrEmpty(_obs.CurrentSceneName)
                ? ""
                : $"Current scene: {_obs.CurrentSceneName}";
        }
        else
        {
            _statusLabel.Text = "⚪ " + _obs.StatusMessage;
            _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
            _sceneLabel.Text = "";
        }
    }
}
