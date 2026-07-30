namespace Wadevo.Controls;

using System.Text.Json;

public sealed class WadevoDesignerPresetStore
{
    private const string FolderName = "Wadevo";
    private const string FileName = "designer-presets.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public WadevoDesignerPresetStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folderPath = Path.Combine(appData, FolderName);

        Directory.CreateDirectory(folderPath);

        _filePath = Path.Combine(folderPath, FileName);
    }

    public List<WadevoDesignerPresetModel> LoadAll()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<WadevoDesignerPresetModel>();
            }

            string json = File.ReadAllText(_filePath);

            List<WadevoDesignerPresetModel>? presets =
                JsonSerializer.Deserialize<List<WadevoDesignerPresetModel>>(json, JsonOptions);

            presets ??= new List<WadevoDesignerPresetModel>();

            // "Now Playing" was renamed to "Song ID" - this normalizes any preset saved
            // before the rename, so existing overlays keep working under their new name
            // instead of silently failing to match anywhere OverlayType gets compared.
            foreach (WadevoDesignerPresetModel preset in presets)
            {
                if (preset.OverlayType == "Now Playing")
                {
                    preset.OverlayType = "Song ID";
                }
            }

            return presets;
        }
        catch
        {
            return new List<WadevoDesignerPresetModel>();
        }
    }

    public WadevoDesignerPresetModel SavePreset(
        string name,
        string overlayType,
        IEnumerable<WadevoDesignerElementState> elements,
        WadevoOverlayStyleSettings? style = null)
    {
        style ??= new WadevoOverlayStyleSettings();

        List<WadevoDesignerPresetModel> presets = LoadAll();

        WadevoDesignerPresetModel preset = new()
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Untitled Layout" : name.Trim(),
            OverlayType = overlayType,
            BackgroundImagePath = style.BackgroundImagePath,
            BackgroundScaleMode = style.BackgroundScaleMode,
            BackgroundRoundedCorners = style.BackgroundRoundedCorners,
            BackgroundWidthPercent = style.BackgroundWidthPercent,
            BackgroundHeightPercent = style.BackgroundHeightPercent,
            BackgroundOpacityPercent = style.BackgroundOpacityPercent,
            BackgroundOffsetX = style.BackgroundOffsetX,
            BackgroundOffsetY = style.BackgroundOffsetY,
            AnimationType = style.AnimationType,
            AnimationDurationMs = style.AnimationDurationMs,
            AutoHideSeconds = style.AutoHideSeconds,
            AlwaysOn = style.AlwaysOn,
            SavedAtUtc = DateTime.UtcNow,
            Elements = CloneElements(elements)
        };

        presets.Add(preset);
        SaveAll(presets);

        return preset;
    }

    public bool RenamePreset(string id, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        List<WadevoDesignerPresetModel> presets = LoadAll();
        WadevoDesignerPresetModel? existing = presets.FirstOrDefault(preset => preset.Id == id);

        if (existing is null)
        {
            return false;
        }

        existing.Name = newName.Trim();

        SaveAll(presets);

        return true;
    }

    public bool UpdatePreset(
        string id,
        IEnumerable<WadevoDesignerElementState> elements,
        WadevoOverlayStyleSettings? style = null)
    {
        style ??= new WadevoOverlayStyleSettings();

        List<WadevoDesignerPresetModel> presets = LoadAll();
        WadevoDesignerPresetModel? existing = presets.FirstOrDefault(preset => preset.Id == id);

        if (existing is null)
        {
            return false;
        }

        existing.Elements = CloneElements(elements);
        existing.BackgroundImagePath = style.BackgroundImagePath;
        existing.BackgroundScaleMode = style.BackgroundScaleMode;
        existing.BackgroundRoundedCorners = style.BackgroundRoundedCorners;
        existing.BackgroundWidthPercent = style.BackgroundWidthPercent;
        existing.BackgroundHeightPercent = style.BackgroundHeightPercent;
        existing.BackgroundOpacityPercent = style.BackgroundOpacityPercent;
        existing.BackgroundOffsetX = style.BackgroundOffsetX;
        existing.BackgroundOffsetY = style.BackgroundOffsetY;
        existing.AnimationType = style.AnimationType;
        existing.AnimationDurationMs = style.AnimationDurationMs;
        existing.AutoHideSeconds = style.AutoHideSeconds;
        existing.AlwaysOn = style.AlwaysOn;
        existing.SavedAtUtc = DateTime.UtcNow;

        SaveAll(presets);

        return true;
    }

    public bool DeletePreset(string id)
    {
        List<WadevoDesignerPresetModel> presets = LoadAll();

        int removed = presets.RemoveAll(preset => preset.Id == id);

        if (removed == 0)
        {
            return false;
        }

        SaveAll(presets);
        return true;
    }

    private void SaveAll(List<WadevoDesignerPresetModel> presets)
    {
        try
        {
            string json = JsonSerializer.Serialize(presets, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Persistence should never break the designer.
        }
    }

    private static List<WadevoDesignerElementState> CloneElements(IEnumerable<WadevoDesignerElementState> elements)
    {
        return elements.Select(element => new WadevoDesignerElementState
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = element.Name,
            Kind = element.Kind,
            X = element.X,
            Y = element.Y,
            Width = element.Width,
            Height = element.Height,
            Text = element.Text,
            FontFamily = element.FontFamily,
            FontSize = element.FontSize,
            FontBold = element.FontBold,
            FontColorArgb = element.FontColorArgb,
            ArtworkUrl = element.ArtworkUrl,
            ImagePath = element.ImagePath,
            CountdownTargetUtc = element.CountdownTargetUtc,
            CountdownLabel = element.CountdownLabel,
            CountdownCompletedText = element.CountdownCompletedText,
            ClockFormat = element.ClockFormat,
            SongQueueMaxVisible = element.SongQueueMaxVisible,
            GoalMetric = element.GoalMetric,
            GoalTarget = element.GoalTarget,
            ProgressFillColorArgb = element.ProgressFillColorArgb,
            ProgressTrackColorArgb = element.ProgressTrackColorArgb,
            IsVisible = element.IsVisible,
            IsLocked = element.IsLocked
        }).ToList();
    }
}
