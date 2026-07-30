using Wadevo.Core;

namespace Wadevo.Controls;

public class WadevoSplash : UserControl
{
    private Label _status = null!;

    private WadevoLogo _logo = null!;

    public WadevoSplash()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Black;

        BuildLayout();
    }

    private void BuildLayout()
    {
        Controls.Clear();

        _logo = new()
        {
            Width = 700,
            Height = 500,
            Location = new Point((Width - 700) / 2, 150)
        };

        _status = new()
        {
            Text = "Starting Wadevo...",
            Width = 700,
            Height = 40,
            Location = new Point((Width - 700) / 2, 760),
            ForeColor = WadevoTheme.Colors.Accent,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(_status);
        Controls.Add(_logo);

        Resize += (_, _) =>
        {
            _logo.Location = new Point(
                (Width - _logo.Width) / 2,
                150);

            _status.Location = new Point(
                (Width - _status.Width) / 2,
                760);
        };
    }

    public void SetStatus(string message)
    {
        if (_status.InvokeRequired)
        {
            _status.BeginInvoke(new MethodInvoker(() => SetStatus(message)));
            return;
        }

        _status.Text = message;
    }
}