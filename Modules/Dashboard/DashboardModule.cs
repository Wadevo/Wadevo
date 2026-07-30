namespace Wadevo.Modules.Dashboard;

using Wadevo.Core;
using Wadevo.Modules;

public class DashboardModule : WadevoModule
{
    public override string ModuleName => "Dashboard";
    public override string ModuleDescription => $"Welcome to {WadevoBrand.AppName}.";

    public DashboardModule()
    {
        Label title = new()
        {
            Text = "Dashboard",
            Font = WadevoTheme.Fonts.Hero,
            AutoSize = true,
            Location = new Point(20, 20)
        };

        Label subtitle = new()
        {
            Text = $"Welcome to {WadevoBrand.AppName}.",
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextSecondary,
            AutoSize = true,
            Location = new Point(22, 85)
        };

        Controls.Add(title);
        Controls.Add(subtitle);
    }
}