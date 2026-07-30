namespace Wadevo.Services.Soundboard;

/// <summary>
/// Copies imported MP3/WAV files into a dedicated Wadevo folder so a soundboard clip keeps
/// working even if the original file gets moved, renamed, or deleted (e.g. it lived on a
/// USB drive or in a Downloads folder someone later cleans out). Mirrors the same pattern
/// GifDownloadService uses for cached GIFs.
/// </summary>
public sealed class SoundLibraryService
{
    private readonly string _libraryFolder;

    public SoundLibraryService()
    {
        _libraryFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Wadevo",
            "Sounds");

        Directory.CreateDirectory(_libraryFolder);
    }

    public string LibraryFolder => _libraryFolder;

    public string Import(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Sound file not found.", sourceFilePath);
        }

        string extension = Path.GetExtension(sourceFilePath);
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string destinationPath = Path.Combine(_libraryFolder, fileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: false);

        return destinationPath;
    }

    public void Remove(string filePath)
    {
        try
        {
            if (File.Exists(filePath) && filePath.StartsWith(_libraryFolder, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Removal is best-effort - an orphaned file in the library folder isn't worth
            // surfacing an error for.
        }
    }
}
