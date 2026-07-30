namespace Wadevo.Models;

public enum ConnectionCategory
{
    Streaming,
    Music,
    Software
}

public enum ConnectionState
{
    Connected,
    Warning,
    NotConnected,
    ComingSoon
}

public sealed class ConnectionInfoModel
{
    public string Name { get; set; } = "";

    public string Glyph { get; set; } = "🔌";

    public ConnectionCategory Category { get; set; }

    public ConnectionState State { get; set; } = ConnectionState.NotConnected;

    public string StatusText { get; set; } = "Not connected";

    public string Description { get; set; } = "";

    public bool CanOpen { get; set; }
}
