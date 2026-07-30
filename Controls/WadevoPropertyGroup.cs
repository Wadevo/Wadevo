namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public sealed class WadevoPropertyGroup : WadevoGlassCard
{
    private readonly Label _titleLabel = new();
    private readonly FlowLayoutPanel _contentPanel = new();

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string GroupTitle
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public FlowLayoutPanel ContentPanel => _contentPanel;

    public WadevoPropertyGroup()
    {
        Width = 320;
        Height = 320;
        Padding = new Padding(0);
        AccentColor = WadevoTheme.Colors.Cyan;

        _titleLabel.Text = "Properties";
        _titleLabel.Location = new Point(18, 16);
        _titleLabel.Size = new Size(260, 26);
        _titleLabel.Font = WadevoTheme.Fonts.Bold;
        _titleLabel.ForeColor = WadevoTheme.Colors.Cyan;
        _titleLabel.BackColor = Color.Transparent;

        _contentPanel.Location = new Point(18, 52);
        _contentPanel.Size = new Size(284, 248);
        _contentPanel.FlowDirection = FlowDirection.TopDown;
        _contentPanel.WrapContents = false;
        _contentPanel.AutoScroll = true;
        _contentPanel.BackColor = Color.Transparent;
        _contentPanel.Margin = new Padding(0);
        _contentPanel.Padding = new Padding(0);

        Controls.Add(_titleLabel);
        Controls.Add(_contentPanel);
    }

    public void AddControl(Control control)
    {
        control.Margin = new Padding(0, 0, 0, 10);
        _contentPanel.Controls.Add(control);
    }

    public void ClearControls()
    {
        _contentPanel.Controls.Clear();
    }
}