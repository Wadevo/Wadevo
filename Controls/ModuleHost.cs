namespace Wadevo.Controls;

using Wadevo.Core;

public class ModuleHost : Panel
{
    private Control? _currentModule;

    public ModuleHost()
    {
        Dock = DockStyle.Fill;
        BackColor = WadevoTheme.Colors.Background;
        Padding = new Padding(0);
        Margin = new Padding(0);
        AutoScroll = true;
    }

    public void ShowModule(Control module)
    {
        if (_currentModule == module)
            return;

        Controls.Clear();

        _currentModule = module;
        _currentModule.Dock = DockStyle.Fill;
        _currentModule.Margin = new Padding(0);
        _currentModule.Padding = new Padding(0);

        Controls.Add(_currentModule);
    }

    public void ClearModule()
    {
        Controls.Clear();
        _currentModule = null;
    }
}