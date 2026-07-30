namespace Wadevo.Services;

using System.Text.Json;
using Wadevo.Models;

public sealed class ChatOverlaySettingsStore
{
    private readonly string _filePath;

    public ChatOverlaySettingsStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "chat-overlay-settings.json");
    }

    public ChatOverlaySettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new ChatOverlaySettings();
            }

            string json = File.ReadAllText(_filePath);
            ChatOverlaySettings? settings = JsonSerializer.Deserialize<ChatOverlaySettings>(json);

            return settings ?? new ChatOverlaySettings();
        }
        catch
        {
            return new ChatOverlaySettings();
        }
    }

    public void Save(ChatOverlaySettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
