namespace Wadevo.Services.Soundboard;

using NAudio.Wave;

/// <summary>
/// Plays local MP3/WAV clips using NAudio. WinForms' built-in SoundPlayer only handles WAV,
/// so this uses NAudio's AudioFileReader (format auto-detected from the file) instead.
///
/// NAudio was chosen over pulling in WPF's MediaPlayer specifically to avoid enabling
/// UseWPF alongside UseWindowsForms in the same project - combining both frameworks injects
/// implicit global usings with colliding type names (Cursor, Application, MessageBox, and
/// so on), which would break the existing WinForms codebase's build. NAudio is a plain
/// NuGet package with no such conflict.
///
/// Each Play() call gets its own output device rather than reusing one. A soundboard needs
/// to layer fast repeated presses - two clips overlapping, or the same clip triggered twice
/// in quick succession - rather than cutting off or queuing behind whatever's already
/// playing.
/// </summary>
public sealed class SoundPlaybackService
{
    private readonly List<(WaveOutEvent Output, AudioFileReader Reader)> _active = new();
    private readonly object _lock = new();

    // Empty/null means "system default", matching the app's existing behavior before a
    // device selector existed. A name is used rather than a raw NAudio device index for
    // the persisted setting, since indices aren't stable across reboots or when USB audio
    // devices get plugged/unplugged in a different order - resolving by name at the moment
    // of playback is what actually stays correct over time.
    public static IReadOnlyList<string> GetAvailableDeviceNames()
    {
        List<string> names = new();

        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            names.Add(WaveOut.GetCapabilities(i).ProductName);
        }

        return names;
    }

    private static int ResolveDeviceNumber(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return -1;
        }

        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            if (string.Equals(WaveOut.GetCapabilities(i).ProductName, deviceName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        // The previously-chosen device isn't currently available (unplugged, renamed,
        // etc.) - falling back to the system default keeps sounds playing rather than
        // silently failing until the user notices and fixes the setting.
        return -1;
    }

    public void Play(string filePath, int volumePercent, string? deviceName = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Sound file not found.", filePath);
        }

        AudioFileReader reader = new(filePath)
        {
            Volume = Math.Clamp(volumePercent, 0, 100) / 100f
        };

        WaveOutEvent output = new()
        {
            DeviceNumber = ResolveDeviceNumber(deviceName)
        };

        output.Init(reader);

        lock (_lock)
        {
            _active.Add((output, reader));
        }

        output.PlaybackStopped += (_, _) => CleanUp(output, reader);

        output.Play();
    }

    public void StopAll()
    {
        List<(WaveOutEvent Output, AudioFileReader Reader)> snapshot;

        lock (_lock)
        {
            snapshot = _active.ToList();
        }

        // Stop() triggers PlaybackStopped asynchronously, which handles the actual
        // disposal/removal - this just requests every active clip to stop now.
        foreach ((WaveOutEvent output, _) in snapshot)
        {
            output.Stop();
        }
    }

    private void CleanUp(WaveOutEvent output, AudioFileReader reader)
    {
        lock (_lock)
        {
            _active.RemoveAll(entry => ReferenceEquals(entry.Output, output));
        }

        output.Dispose();
        reader.Dispose();
    }
}
