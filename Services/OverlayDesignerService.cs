namespace Wadevo.Services;

using Wadevo.Models;

public sealed class OverlayDesignerService
{
    private readonly OverlayThemeService _themeService = new();
    private readonly OverlaySettingsService _settingsService = new();

    public OverlayThemeModel GetSelectedTheme()
    {
        OverlaySettingsModel settings = _settingsService.Load();

        return _themeService.GetTheme(settings.SelectedThemeId);
    }

    public void SelectTheme(string themeId)
    {
        OverlaySettingsModel settings = _settingsService.Load();

        settings.SelectedThemeId = themeId;

        _settingsService.Save(settings);
    }

    public void UpdatePreview(
        string title,
        string artist,
        string song)
    {
        OverlaySettingsModel settings = _settingsService.Load();

        settings.NowPlayingTitle = title;
        settings.SampleArtist = artist;
        settings.SampleSong = song;

        _settingsService.Save(settings);
    }

    public void UpdateAlertPreview(
        string title,
        string message,
        int durationMilliseconds)
    {
        OverlaySettingsModel settings = _settingsService.Load();

        settings.AlertTitle = title;
        settings.AlertMessage = message;
        settings.AlertDurationMilliseconds = durationMilliseconds;

        _settingsService.Save(settings);
    }

    public void UpdateCommandPreview(
        string title,
        string message,
        int durationMilliseconds)
    {
        OverlaySettingsModel settings = _settingsService.Load();

        settings.CommandTitle = title;
        settings.CommandMessage = message;
        settings.CommandDurationMilliseconds = durationMilliseconds;

        _settingsService.Save(settings);
    }

    public OverlaySettingsModel GetPreviewSettings()
    {
        return _settingsService.Load();
    }

    public void Reset()
    {
        _settingsService.Reset();
    }
}