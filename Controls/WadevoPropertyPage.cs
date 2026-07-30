namespace Wadevo.Controls;

using Wadevo.Core;

public sealed class WadevoPropertyPage : UserControl
{
    private readonly WadevoPropertyScrollPanel _scrollPanel = new();

    public WadevoPropertyScrollPanel Properties => _scrollPanel;

    public WadevoPropertyPage()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(20);

        _scrollPanel.Dock = DockStyle.Fill;

        Controls.Add(_scrollPanel);
    }

    public void Add(Control control)
    {
        _scrollPanel.Add(control);
    }

    public void AddRange(params Control[] controls)
    {
        foreach (Control control in controls)
        {
            _scrollPanel.Add(control);
        }
    }

    public void Clear()
    {
        _scrollPanel.ClearItems();
    }
}