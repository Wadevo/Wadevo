namespace Wadevo.Services.Twitch;

using System.Text.Json;

// Solves the same problem BlazeDevCredentialsStore solves: TwitchAppCredentials.cs gets
// regenerated with placeholder text every time a new build of Wadevo's source is
// delivered, since real values are never stored there permanently. This file lives in
// %AppData% instead - completely outside the project - so once it's filled in, it
// survives every future update, and it's never at risk of being committed to a
// (possibly public) git repository.
public sealed class TwitchDevCredentialsStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "twitch-app-credentials.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public TwitchDevCredentialsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public string FilePath => _filePath;

    public bool Exists => File.Exists(_filePath);

    public (string ClientId, string ClientSecret)? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string json = File.ReadAllText(_filePath);
            TwitchDevCredentials? credentials = JsonSerializer.Deserialize<TwitchDevCredentials>(json, JsonOptions);

            if (credentials is null ||
                string.IsNullOrWhiteSpace(credentials.ClientId) ||
                string.IsNullOrWhiteSpace(credentials.ClientSecret))
            {
                return null;
            }

            return (credentials.ClientId, credentials.ClientSecret);
        }
        catch
        {
            return null;
        }
    }

    public void Save(string clientId, string clientSecret)
    {
        try
        {
            TwitchDevCredentials credentials = new()
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            string json = JsonSerializer.Serialize(credentials, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never break the app.
        }
    }

    private sealed class TwitchDevCredentials
    {
        public string ClientId { get; set; } = "";

        public string ClientSecret { get; set; } = "";
    }
}
