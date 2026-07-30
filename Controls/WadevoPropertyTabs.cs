namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoPropertyTabs : Panel
{
    private readonly FlowLayoutPanel _buttonPanel = new();

    private readonly Dictionary<string, WadevoButton> _buttons = new();

    public event Action<string>? TabSelected;

    public WadevoPropertyTabs()
    {
        Height = 48;
        Dock = DockStyle.Top;
        BackColor = Color.Transparent;

        _buttonPanel.Dock = DockStyle.Fill;
        _buttonPanel.FlowDirection = FlowDirection.LeftToRight;
        _buttonPanel.WrapContents = false;
        _buttonPanel.AutoSize = false;
        _buttonPanel.BackColor = Color.Transparent;
        _buttonPanel.Padding = new Padding(0);
        _buttonPanel.Margin = new Padding(0);

        Controls.Add(_buttonPanel);
    }

    public void AddTab(string key, string text)
    {
        WadevoButton button = new()
        {
            ButtonText = text,
            Width = 110,
            Height = 36,
            Margin = new Padding(0, 0, 8, 0),
            AccentColor = WadevoTheme.Colors.Panel
        };

        button.ButtonClicked += (_, _) =>
        {
            SelectTab(key);
            TabSelected?.Invoke(key);
        };

        _buttons[key] = button;
        _buttonPanel.Controls.Add(button);

        if (_buttons.Count == 1)
        {
            SelectTab(key);
        }
    }

    public void SelectTab(string key)
    {
        foreach ((string buttonKey, WadevoButton button) in _buttons)
        {
            button.AccentColor = buttonKey == key
                ? WadevoTheme.Colors.Cyan
                : WadevoTheme.Colors.Panel;
        }
    }
}