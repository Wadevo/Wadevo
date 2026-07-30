namespace Wadevo.Services.Obs;

using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wadevo.Models;

public sealed class ObsConnectionSettingsStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "obs-connection.dat";

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Wadevo.ObsConnection.v1");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public ObsConnectionSettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    [SupportedOSPlatform("windows")]
    public ObsConnectionSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new ObsConnectionSettings();
            }

            byte[] encrypted = File.ReadAllBytes(_filePath);
            byte[] decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(decrypted);
            ObsConnectionSettings? settings = JsonSerializer.Deserialize<ObsConnectionSettings>(json, JsonOptions);

            return settings ?? new ObsConnectionSettings();
        }
        catch
        {
            return new ObsConnectionSettings();
        }
    }

    [SupportedOSPlatform("windows")]
    public void Save(ObsConnectionSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            byte[] plain = Encoding.UTF8.GetBytes(json);
            byte[] encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_filePath, encrypted);
        }
        catch
        {
            // Persistence should never crash the app.
        }
    }
}
