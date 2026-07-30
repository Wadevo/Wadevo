namespace Wadevo.Services.Twitch;

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public sealed class TwitchConnectionStore
{
    private const string FolderName = "Wadevo";
    private const string DefaultFileName = "twitch-connection.dat";

    // Ties the encrypted data to this specific use, so the file can't be decrypted
    // if it's ever copied out and combined with unrelated encrypted data.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Wadevo.TwitchConnection.v1");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public TwitchConnectionStore(string fileName = DefaultFileName)
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, fileName);
    }

    [SupportedOSPlatform("windows")]
    public void Load(TwitchConnectionState state)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            byte[] encrypted = File.ReadAllBytes(_filePath);
            byte[] decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(decrypted);
            TwitchConnectionState? saved = JsonSerializer.Deserialize<TwitchConnectionState>(json, JsonOptions);

            if (saved is null)
            {
                return;
            }

            state.IsAuthenticated = saved.IsAuthenticated;
            state.StatusMessage = saved.StatusMessage;
            state.UserId = saved.UserId;
            state.Username = saved.Username;
            state.AccessToken = saved.AccessToken;
            state.RefreshToken = saved.RefreshToken;
            state.TokenExpiresAt = saved.TokenExpiresAt;
        }
        catch
        {
            // A corrupt file, or one saved by a different Windows user account, just
            // means starting from a clean, disconnected state - never crash on this.
        }
    }

    [SupportedOSPlatform("windows")]
    public void Save(TwitchConnectionState state)
    {
        try
        {
            string json = JsonSerializer.Serialize(state, JsonOptions);
            byte[] plain = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_filePath, encrypted);
        }
        catch
        {
            // Persistence should never break the app; worst case, reconnect next launch.
        }
    }
}
