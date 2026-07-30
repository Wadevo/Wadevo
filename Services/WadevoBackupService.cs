namespace Wadevo.Services;

using System.IO.Compression;

public static class WadevoBackupService
{
    private const string AppFolderName = "Wadevo";

    // User-created content - safe to back up and move between machines.
    private static readonly string[] SafeFiles =
    {
        "commands.json",
        "alert-profiles.json",
        "designer-presets.json",
        "designer-document.json",
        "designer-canvas-state.json",
        "live-overlay-settings.json",
        "overlay-settings.json",
        "overlay-themes.json",
        "app-settings.json"
    };

    private const string CustomFontsFolderName = "CustomFonts";

    // Credentials and API keys - excluded by default. The Blaze connection specifically
    // is DPAPI-encrypted to this Windows user account and machine, so it wouldn't even work
    // if restored on a different computer regardless of whether it's included.
    private static readonly string[] SensitiveFiles =
    {
        "blaze-connection.dat",
        "blaze-app-credentials.json",
        "twitch-connection.dat",
        "twitch-app-credentials.json",
        "gif-settings.json"
    };

    public static string GetWadevoDataFolder()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppFolderName);
    }

    public static BackupExportResult ExportBackup(string destinationZipPath, bool includeSensitive)
    {
        string dataFolder = GetWadevoDataFolder();
        string stagingFolder = Path.Combine(Path.GetTempPath(), $"wadevo-backup-{Guid.NewGuid():N}");

        List<string> includedFiles = new();

        try
        {
            Directory.CreateDirectory(stagingFolder);

            foreach (string fileName in SafeFiles)
            {
                string sourcePath = Path.Combine(dataFolder, fileName);

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, Path.Combine(stagingFolder, fileName));
                    includedFiles.Add(fileName);
                }
            }

            string fontsSource = Path.Combine(dataFolder, CustomFontsFolderName);

            if (Directory.Exists(fontsSource))
            {
                string fontsDestination = Path.Combine(stagingFolder, CustomFontsFolderName);
                CopyDirectory(fontsSource, fontsDestination);
                includedFiles.Add(CustomFontsFolderName + "\\");
            }

            if (includeSensitive)
            {
                foreach (string fileName in SensitiveFiles)
                {
                    string sourcePath = Path.Combine(dataFolder, fileName);

                    if (File.Exists(sourcePath))
                    {
                        File.Copy(sourcePath, Path.Combine(stagingFolder, fileName));
                        includedFiles.Add(fileName);
                    }
                }
            }

            File.WriteAllText(
                Path.Combine(stagingFolder, "backup-manifest.txt"),
                $"Wadevo backup created {DateTime.Now:yyyy-MM-dd HH:mm}\nIncludes credentials: {includeSensitive}");

            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }

            ZipFile.CreateFromDirectory(stagingFolder, destinationZipPath);

            return new BackupExportResult(true, includedFiles, "");
        }
        catch (Exception ex)
        {
            return new BackupExportResult(false, includedFiles, ex.Message);
        }
        finally
        {
            TryDeleteDirectory(stagingFolder);
        }
    }

    public static BackupImportResult ImportBackup(string sourceZipPath)
    {
        string extractFolder = Path.Combine(Path.GetTempPath(), $"wadevo-restore-{Guid.NewGuid():N}");
        List<string> restoredFiles = new();

        try
        {
            if (!File.Exists(sourceZipPath))
            {
                return new BackupImportResult(false, restoredFiles, "That backup file couldn't be found.");
            }

            Directory.CreateDirectory(extractFolder);
            ZipFile.ExtractToDirectory(sourceZipPath, extractFolder);

            bool looksLikeBackup = File.Exists(Path.Combine(extractFolder, "backup-manifest.txt")) ||
                SafeFiles.Any(f => File.Exists(Path.Combine(extractFolder, f)));

            if (!looksLikeBackup)
            {
                return new BackupImportResult(
                    false, restoredFiles, "That file doesn't look like a Wadevo backup.");
            }

            string dataFolder = GetWadevoDataFolder();
            Directory.CreateDirectory(dataFolder);

            foreach (string fileName in SafeFiles.Concat(SensitiveFiles))
            {
                string sourcePath = Path.Combine(extractFolder, fileName);

                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, Path.Combine(dataFolder, fileName), overwrite: true);
                    restoredFiles.Add(fileName);
                }
            }

            string fontsSource = Path.Combine(extractFolder, CustomFontsFolderName);

            if (Directory.Exists(fontsSource))
            {
                string fontsDestination = Path.Combine(dataFolder, CustomFontsFolderName);
                CopyDirectory(fontsSource, fontsDestination);
                restoredFiles.Add(CustomFontsFolderName + "\\");
            }

            return new BackupImportResult(true, restoredFiles, "");
        }
        catch (Exception ex)
        {
            return new BackupImportResult(false, restoredFiles, ex.Message);
        }
        finally
        {
            TryDeleteDirectory(extractFolder);
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (string filePath in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(filePath);
            File.Copy(filePath, Path.Combine(destinationDir, fileName), overwrite: true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir))
        {
            string subDirName = Path.GetFileName(subDir);
            CopyDirectory(subDir, Path.Combine(destinationDir, subDirName));
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup - a leftover temp folder isn't worth crashing over.
        }
    }
}

public sealed record BackupExportResult(bool Success, List<string> IncludedFiles, string ErrorMessage);

public sealed record BackupImportResult(bool Success, List<string> RestoredFiles, string ErrorMessage);
