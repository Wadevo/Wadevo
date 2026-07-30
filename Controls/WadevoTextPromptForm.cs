namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoTextPromptForm : WadevoDialogForm
{
    private readonly TextBox _textBox = new();

    public string InputText => _textBox.Text.Trim();

    public WadevoTextPromptForm(string title, string message, string defaultValue = "") : base(title)
    {
        Size = new Size(420, 222);

        Label messageLabel = new()
        {
            Text = message,
            Location = new Point(20, 20),
            Size = new Size(380, 24),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _textBox.Text = defaultValue;
        _textBox.Location = new Point(20, 52);
        _textBox.Size = new Size(380, 28);
        _textBox.Font = WadevoTheme.Fonts.Default;
        _textBox.ForeColor = WadevoTheme.Colors.Text;
        _textBox.BackColor = WadevoTheme.Colors.Panel;
        _textBox.BorderStyle = BorderStyle.FixedSingle;

        WadevoButton saveButton = new()
        {
            ButtonText = "Save",
            Location = new Point(220, 100),
            Size = new Size(90, 38),
            AccentColor = WadevoTheme.Colors.Accent
        };

        WadevoButton cancelButton = new()
        {
            ButtonText = "Cancel",
            Location = new Point(320, 100),
            Size = new Size(80, 38),
            AccentColor = WadevoTheme.Colors.TextMuted
        };

        saveButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        cancelButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        ContentPanel.Controls.Add(messageLabel);
        ContentPanel.Controls.Add(_textBox);
        ContentPanel.Controls.Add(saveButton);
        ContentPanel.Controls.Add(cancelButton);

        KeyPreview = true;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        Shown += (_, _) =>
        {
            _textBox.Focus();
            _textBox.SelectAll();
        };
    }
}
