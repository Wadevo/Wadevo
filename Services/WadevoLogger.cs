namespace Wadevo.Services;

public static class WadevoLogger
{
    private const string FolderName = "Wadevo";
    private const string FileName = "wadevo.log";
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    private static readonly object WriteLock = new();
    private static readonly string FilePath;

    static WadevoLogger()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        FilePath = Path.Combine(folderPath, FileName);
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warning(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
    {
        string fullMessage = exception is null
            ? message
            : $"{message} — {exception.GetType().Name}: {exception.Message}";

        Write("ERROR", fullMessage);
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (WriteLock)
            {
                TrimIfTooLarge();

                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level,-5} {message}";
                File.AppendAllLines(FilePath, new[] { line });
            }
        }
        catch
        {
            // Logging should never crash the app it's trying to help debug.
        }
    }

    private static void TrimIfTooLarge()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        FileInfo info = new(FilePath);

        if (info.Length <= MaxFileSizeBytes)
        {
            return;
        }

        // Keep the most recent half of the log rather than growing forever.
        string[] lines = File.ReadAllLines(FilePath);
        int keepFrom = lines.Length / 2;

        File.WriteAllLines(FilePath, lines.Skip(keepFrom));
    }
}
