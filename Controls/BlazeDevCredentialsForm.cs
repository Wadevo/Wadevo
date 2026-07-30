namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Services.Blaze;

public sealed class BlazeDevCredentialsForm : WadevoDialogForm
{
    private readonly TextBox _clientIdTextBox = new();
    private readonly TextBox _clientSecretTextBox = new();

    public BlazeDevCredentialsForm() : base("Blaze App Setup")
    {
        Size = new Size(460, 332);

        Label message = new()
        {
            Text = "This is a one-time setup. These are saved to a file outside the project,\n" +
                   "so future updates to Wadevo's code will never overwrite them again.",
            Location = new Point(24, 20),
            Size = new Size(412, 44),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        Label clientIdLabel = new()
        {
            Text = "Client ID",
            Location = new Point(24, 76),
            Size = new Size(200, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        _clientIdTextBox.Location = new Point(24, 98);
        _clientIdTextBox.Size = new Size(412, 28);
        _clientIdTextBox.Font = WadevoTheme.Fonts.Default;
        _clientIdTextBox.ForeColor = WadevoTheme.Colors.Text;
        _clientIdTextBox.BackColor = WadevoTheme.Colors.Panel;
        _clientIdTextBox.BorderStyle = BorderStyle.FixedSingle;

        Label clientSecretLabel = new()
        {
            Text = "Client Secret",
            Location = new Point(24, 136),
            Size = new Size(200, 20),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        _clientSecretTextBox.Location = new Point(24, 158);
        _clientSecretTextBox.Size = new Size(412, 28);
        _clientSecretTextBox.Font = WadevoTheme.Fonts.Default;
        _clientSecretTextBox.ForeColor = WadevoTheme.Colors.Text;
        _clientSecretTextBox.BackColor = WadevoTheme.Colors.Panel;
        _clientSecretTextBox.BorderStyle = BorderStyle.FixedSingle;
        _clientSecretTextBox.UseSystemPasswordChar = true;

        WadevoButton saveButton = new()
        {
            ButtonText = "Save",
            Location = new Point(242, 210),
            Size = new Size(90, 38),
            AccentColor = WadevoTheme.Colors.Accent
        };

        WadevoButton cancelButton = new()
        {
            ButtonText = "Cancel",
            Location = new Point(344, 210),
            Size = new Size(92, 38),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        saveButton.ButtonClicked += (_, _) =>
        {
            string clientId = _clientIdTextBox.Text.Trim();
            string clientSecret = _clientSecretTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                WadevoMessageBox.Show(this, "Both fields are required.", "Blaze App Setup");
                return;
            }

            new BlazeDevCredentialsStore().Save(clientId, clientSecret);

            DialogResult = DialogResult.OK;
            Close();
        };

        cancelButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        KeyPreview = true;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        ContentPanel.Controls.Add(message);
        ContentPanel.Controls.Add(clientIdLabel);
        ContentPanel.Controls.Add(_clientIdTextBox);
        ContentPanel.Controls.Add(clientSecretLabel);
        ContentPanel.Controls.Add(_clientSecretTextBox);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(cancelButton);

        Shown += (_, _) => _clientIdTextBox.Focus();
    }
}
