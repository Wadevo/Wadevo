namespace Wadevo.Modules;

using Wadevo.Core;

public class WadevoModule : UserControl
{
    public virtual string ModuleName => "Module";
    public virtual string ModuleDescription => "";

    public WadevoModule()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Background;
        ForeColor = WadevoTheme.Colors.Text;
        Font = WadevoTheme.Fonts.Default;
        Padding = new Padding(40);
        Margin = new Padding(0);
    }
}