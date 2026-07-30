namespace Wadevo.Controls;

public sealed class WadevoPropertyScrollPanel : Panel
{
    private readonly WadevoScrollablePanel _scrollablePanel = new();

    public FlowLayoutPanel FlowPanel => _scrollablePanel.Content;

    public WadevoPropertyScrollPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;
        Padding = new Padding(0);

        AutoScroll = false;

        _scrollablePanel.Dock = DockStyle.Fill;
        _scrollablePanel.BackColor = Color.Transparent;
        _scrollablePanel.Content.Padding = new Padding(0);

        Controls.Add(_scrollablePanel);
    }

    public void Add(Control control)
    {
        control.Margin = new Padding(0, 0, 0, 10);
        _scrollablePanel.Content.Controls.Add(control);
        _scrollablePanel.RefreshLayout();
    }

    public void AddRange(IEnumerable<Control> controls)
    {
        foreach (Control control in controls)
        {
            control.Margin = new Padding(0, 0, 0, 10);
            _scrollablePanel.Content.Controls.Add(control);
        }

        _scrollablePanel.RefreshLayout();
    }

    public void ClearItems()
    {
        _scrollablePanel.Content.Controls.Clear();
        _scrollablePanel.RefreshLayout();
    }
}