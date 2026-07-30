namespace Wadevo.Controls;

using Wadevo.Core;

public static class WadevoMessageBox
{
    public static void Show(IWin32Window? owner, string message, string title = "Wadevo")
    {
        using WadevoMessageForm form = new(title, message, showCancel: false);
        form.ShowDialog(owner);
    }

    public static bool Confirm(IWin32Window? owner, string message, string title = "Wadevo")
    {
        using WadevoMessageForm form = new(title, message, showCancel: true);
        return form.ShowDialog(owner) == DialogResult.OK;
    }
}

internal sealed class WadevoMessageForm : WadevoDialogForm
{
    public WadevoMessageForm(string title, string message, bool showCancel) : base(title)
    {
        Size = new Size(440, 232);

        Label messageLabel = new()
        {
            Text = message,
            Location = new Point(24, 24),
            Size = new Size(392, 96),
            Font = WadevoTheme.Fonts.Default,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent
        };

        WadevoButton okButton = new()
        {
            ButtonText = showCancel ? "Yes" : "OK",
            Location = new Point(showCancel ? 220 : 330, 128),
            Size = new Size(90, 38),
            AccentColor = WadevoTheme.Colors.Accent
        };

        okButton.ButtonClicked += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        ContentPanel.Controls.Add(messageLabel);
        ContentPanel.Controls.Add(okButton);

        if (showCancel)
        {
            WadevoButton cancelButton = new()
            {
                ButtonText = "No",
                Location = new Point(322, 128),
                Size = new Size(90, 38),
                AccentColor = WadevoTheme.Colors.TextMuted
            };

            cancelButton.ButtonClicked += (_, _) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            ContentPanel.Controls.Add(cancelButton);
        }

        KeyPreview = true;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };
    }
}
