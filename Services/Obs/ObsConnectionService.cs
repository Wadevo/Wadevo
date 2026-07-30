namespace Wadevo.Services.Obs;

using System.Text.Json;
using Wadevo.Models;
using Wadevo.Services.Blaze;
using Wadevo.Services.Twitch;

public sealed class ObsConnectionService
{
    private static readonly Lazy<ObsConnectionService> LazyShared = new(() => new ObsConnectionService());

    public static ObsConnectionService Shared => LazyShared.Value;

    private readonly ObsWebSocketClient _client = new();
    private readonly ObsConnectionSettingsStore _store = new();

    public ObsConnectionSettings Settings { get; private set; }

    public bool IsConnected => _client.IsConnected;

    public bool IsStreaming { get; private set; }

    public string CurrentSceneName { get; private set; } = "";

    public string StatusMessage { get; private set; } = "Not connected";

    public event EventHandler? StateChanged;

    // Fired specifically when OBS reports streaming has started - this is what lets other
    // parts of Wadevo (like auto-starting Twitch/Blaze event listeners) react to "the
    // broadcast just went live" without needing their own polling.
    public event EventHandler? StreamingStarted;

    public event EventHandler? StreamingStopped;

    private ObsConnectionService()
    {
        Settings = _store.Load();

        _client.EventReceived += OnEventReceived;
        _client.Disconnected += (_, _) =>
        {
            IsStreaming = false;
            StatusMessage = "Disconnected";
            StateChanged?.Invoke(this, EventArgs.Empty);
        };

        // The main practical payoff of knowing OBS's streaming state: the moment the
        // broadcast actually goes live, make sure both platforms' event listeners are
        // running too, instead of relying on the person remembering to click "Start
        // Events" on each Connections popup separately every time they go live.
        StreamingStarted += (_, _) =>
        {
            _ = TwitchLiveEventService.Shared.StartAsync();
            _ = BlazeLiveEventService.Shared.StartAsync();
        };

        if (Settings.AutoConnect)
        {
            _ = ConnectAsync();
        }
    }

    public async Task ConnectAsync()
    {
        try
        {
            StatusMessage = "Connecting...";
            StateChanged?.Invoke(this, EventArgs.Empty);

            await _client.ConnectAsync(Settings.Host, Settings.Port, Settings.Password);

            StatusMessage = "Connected";

            await RefreshCurrentSceneAsync();
            await RefreshStreamingStateAsync();

            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
            StateChanged?.Invoke(this, EventArgs.Empty);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        await _client.DisconnectAsync();
        IsStreaming = false;
        StatusMessage = "Not connected";
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSettings(ObsConnectionSettings settings)
    {
        Settings = settings;
        _store.Save(settings);
    }

    public async Task<IReadOnlyList<string>> GetSceneListAsync()
    {
        JsonElement response = await _client.SendRequestAsync("GetSceneList");

        List<string> scenes = new();

        if (response.TryGetProperty("scenes", out JsonElement sceneArray))
        {
            foreach (JsonElement scene in sceneArray.EnumerateArray())
            {
                if (scene.TryGetProperty("sceneName", out JsonElement name))
                {
                    scenes.Add(name.GetString() ?? "");
                }
            }
        }

        scenes.Reverse(); // OBS returns scenes in reverse display order.
        return scenes;
    }

    public async Task SetCurrentSceneAsync(string sceneName)
    {
        await _client.SendRequestAsync("SetCurrentProgramScene", new { sceneName });
        CurrentSceneName = sceneName;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task StartStreamAsync()
    {
        await _client.SendRequestAsync("StartStream");
    }

    public async Task StopStreamAsync()
    {
        await _client.SendRequestAsync("StopStream");
    }

    private async Task RefreshCurrentSceneAsync()
    {
        try
        {
            JsonElement response = await _client.SendRequestAsync("GetCurrentProgramScene");

            if (response.TryGetProperty("sceneName", out JsonElement name))
            {
                CurrentSceneName = name.GetString() ?? "";
            }
        }
        catch
        {
            // Non-critical - the UI will just show a blank current scene until the next event.
        }
    }

    private async Task RefreshStreamingStateAsync()
    {
        try
        {
            JsonElement response = await _client.SendRequestAsync("GetStreamStatus");

            if (response.TryGetProperty("outputActive", out JsonElement active))
            {
                IsStreaming = active.GetBoolean();
            }
        }
        catch
        {
            // Non-critical - the UI will just show not-streaming until the next event.
        }
    }

    private void OnEventReceived(object? sender, (string EventType, JsonElement EventData) e)
    {
        switch (e.EventType)
        {
            case "StreamStateChanged":
            {
                bool wasStreaming = IsStreaming;

                if (e.EventData.TryGetProperty("outputActive", out JsonElement active))
                {
                    IsStreaming = active.GetBoolean();
                }

                StateChanged?.Invoke(this, EventArgs.Empty);

                if (IsStreaming && !wasStreaming)
                {
                    StreamingStarted?.Invoke(this, EventArgs.Empty);
                }
                else if (!IsStreaming && wasStreaming)
                {
                    StreamingStopped?.Invoke(this, EventArgs.Empty);
                }

                break;
            }

            case "CurrentProgramSceneChanged":
            {
                if (e.EventData.TryGetProperty("sceneName", out JsonElement name))
                {
                    CurrentSceneName = name.GetString() ?? "";
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }

                break;
            }
        }
    }
}
