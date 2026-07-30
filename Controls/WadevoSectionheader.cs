namespace Wadevo.Controls;

using System.ComponentModel;
using Wadevo.Core;

public sealed class WadevoSectionHeader : Label
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string HeaderText
    {
        get => Text;
        set => Text = value;
    }

    public WadevoSectionHeader()
    {
        Size = new Size(260, 24);
        Font = WadevoTheme.Fonts.Bold;
        ForeColor = WadevoTheme.Colors.Cyan;
        BackColor = Color.Transparent;
        TextAlign = ContentAlignment.MiddleLeft;
    }
}