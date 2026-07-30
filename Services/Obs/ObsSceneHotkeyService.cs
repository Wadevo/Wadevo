namespace Wadevo.Services.Obs;

using Wadevo.Models;
using Wadevo.Services.Soundboard;

// Registers global scene-switch hotkeys with Windows itself, the same way Soundboard clips
// do (reuses GlobalHotkeyService directly). This lives as an app-lifetime singleton rather
// than something owned by the Workspace Studio panel, since the whole point of a hotkey is
// reaching over and hitting it mid-game - it needs to work whether or not Workspace Studio
// (or even Wadevo's main window) is currently open or focused.
public sealed class ObsSceneHotkeyService
{
    private static readonly Lazy<ObsSceneHotkeyService> LazyShared = new(() => new ObsSceneHotkeyService());

    public static ObsSceneHotkeyService Shared => LazyShared.Value;

    private readonly GlobalHotkeyService _hotkeyService = new();
    private readonly ObsSceneHotkeyStore _store = new();
    private readonly Dictionary<Guid, string> _idToScene = new();
    private List<ObsSceneHotkeyModel> _bindings;

    public event EventHandler? BindingsChanged;

    private ObsSceneHotkeyService()
    {
        _bindings = _store.Load();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        RegisterAllSavedBindings();
    }

    public IReadOnlyList<ObsSceneHotkeyModel> GetBindings()
    {
        return _bindings;
    }

    // Returns false (with no changes made) if the combo is already in use by another
    // binding or another app - the caller should tell the user to pick a different combo.
    public bool SetHotkey(string sceneName, GlobalHotkeyService.HotkeyModifiers modifiers, Keys key)
    {
        Guid bindingId = GetOrCreateBindingId(sceneName);

        bool registered = _hotkeyService.Register(bindingId, modifiers, key);

        if (!registered)
        {
            return false;
        }

        ObsSceneHotkeyModel binding = _bindings.First(b => b.SceneName == sceneName);
        binding.HotkeyModifiers = HotkeyFormatting.FormatModifiers(modifiers);
        binding.HotkeyKey = key.ToString();

        _store.Save(_bindings);
        BindingsChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    public void ClearHotkey(string sceneName)
    {
        ObsSceneHotkeyModel? binding = _bindings.FirstOrDefault(b => b.SceneName == sceneName);

        if (binding is null)
        {
            return;
        }

        Guid bindingId = _idToScene.FirstOrDefault(kvp => kvp.Value == sceneName).Key;

        if (bindingId != Guid.Empty)
        {
            _hotkeyService.Unregister(bindingId);
        }

        binding.HotkeyModifiers = "";
        binding.HotkeyKey = "";

        _store.Save(_bindings);
        BindingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RegisterAllSavedBindings()
    {
        foreach (ObsSceneHotkeyModel binding in _bindings.Where(b => b.HasHotkey))
        {
            Guid bindingId = GetOrCreateBindingId(binding.SceneName);

            GlobalHotkeyService.HotkeyModifiers modifiers = HotkeyFormatting.ParseModifiers(binding.HotkeyModifiers);

            if (Enum.TryParse(binding.HotkeyKey, out Keys key))
            {
                _hotkeyService.Register(bindingId, modifiers, key);
            }
        }
    }

    private Guid GetOrCreateBindingId(string sceneName)
    {
        Guid existingId = _idToScene.FirstOrDefault(kvp => kvp.Value == sceneName).Key;

        if (existingId != Guid.Empty)
        {
            return existingId;
        }

        ObsSceneHotkeyModel? binding = _bindings.FirstOrDefault(b => b.SceneName == sceneName);

        if (binding is null)
        {
            binding = new ObsSceneHotkeyModel { SceneName = sceneName };
            _bindings.Add(binding);
        }

        Guid newId = Guid.NewGuid();
        _idToScene[newId] = sceneName;

        return newId;
    }

    private void OnHotkeyPressed(Guid bindingId)
    {
        if (!_idToScene.TryGetValue(bindingId, out string? sceneName))
        {
            return;
        }

        _ = ObsConnectionService.Shared.SetCurrentSceneAsync(sceneName);
    }
}
