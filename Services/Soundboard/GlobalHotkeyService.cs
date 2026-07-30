namespace Wadevo.Services.Soundboard;

using System.Runtime.InteropServices;

/// <summary>
/// Registers hotkeys with Windows itself (RegisterHotKey), not just with Wadevo's own
/// window. This is what makes a soundboard actually useful mid-game: the whole point is
/// reaching over and hitting a key while a game or browser has focus, without alt-tabbing
/// out to click a button first. A handler tied to Wadevo's own KeyDown event would only
/// fire while Wadevo itself was focused, which defeats the purpose.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [Flags]
    public enum HotkeyModifiers : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,

        // Stops Windows auto-repeating the message while the key is held down - without
        // this, holding a hotkey down fires the same sound repeatedly instead of once.
        NoRepeat = 0x4000
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly HotkeyWindow _window = new();
    private readonly Dictionary<int, Guid> _idToClip = new();
    private readonly Dictionary<Guid, int> _clipToId = new();
    private int _nextId = 1;

    public event Action<Guid>? HotkeyPressed;

    public GlobalHotkeyService()
    {
        _window.HotkeyMessage += id =>
        {
            if (_idToClip.TryGetValue(id, out Guid clipId))
            {
                HotkeyPressed?.Invoke(clipId);
            }
        };
    }

    /// <summary>
    /// Registers a global hotkey for a clip, replacing any existing binding for that clip.
    /// Returns false if Windows couldn't register it - almost always because another app
    /// (or another Wadevo clip) already owns that exact combo.
    /// </summary>
    public bool Register(Guid clipId, HotkeyModifiers modifiers, Keys key)
    {
        Unregister(clipId);

        int id = _nextId++;

        bool registered = RegisterHotKey(
            _window.Handle,
            id,
            (uint)(modifiers | HotkeyModifiers.NoRepeat),
            (uint)key);

        if (!registered)
        {
            return false;
        }

        _idToClip[id] = clipId;
        _clipToId[clipId] = id;

        return true;
    }

    public void Unregister(Guid clipId)
    {
        if (_clipToId.TryGetValue(clipId, out int id))
        {
            UnregisterHotKey(_window.Handle, id);
            _clipToId.Remove(clipId);
            _idToClip.Remove(id);
        }
    }

    public void Dispose()
    {
        foreach (int id in _idToClip.Keys.ToList())
        {
            UnregisterHotKey(_window.Handle, id);
        }

        _idToClip.Clear();
        _clipToId.Clear();
        _window.ReleaseHandle();
    }

    // A message-only native window whose sole job is receiving WM_HOTKEY messages.
    // RegisterHotKey needs a real window handle to deliver to, but that window never
    // needs to be visible - it's just a mailbox for Windows to post to.
    private sealed class HotkeyWindow : NativeWindow
    {
        public event Action<int>? HotkeyMessage;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                HotkeyMessage?.Invoke(m.WParam.ToInt32());
            }

            base.WndProc(ref m);
        }
    }
}
