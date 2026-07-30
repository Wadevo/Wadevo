namespace Wadevo.Controls;

using Wadevo.Services;

/// <summary>
/// The single source of truth for "what fonts show up in a font picker, and how does
/// uploading a custom one work" - used by every widget style editor that has a font
/// dropdown. Before this existed, each editor had its own copy of this logic, and they'd
/// drifted apart: some had the full list plus upload, some only had the bare system font
/// list with no way to add a custom one. Routing every editor through here means that
/// class of inconsistency can't happen again - there's only one place this logic lives.
/// </summary>
public static class WadevoFontPickerHelper
{
    public const string UploadFontOption = "+ Upload Custom Font...";

    public static void PopulateFontCombo(ComboBox combo, string currentFont)
    {
        combo.Items.Clear();

        foreach (string fontName in CustomFontService.GetAllFontNames())
        {
            combo.Items.Add(fontName);
        }

        combo.Items.Add(UploadFontOption);

        int existingIndex = combo.Items.IndexOf(currentFont);
        combo.SelectedIndex = existingIndex >= 0 ? existingIndex : 0;
    }

    // Wires the combo so picking "+ Upload Custom Font..." triggers the file picker
    // automatically - callers just need to populate the combo and call this once.
    public static void WireUploadOption(ComboBox combo, IWin32Window owner)
    {
        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem?.ToString() == UploadFontOption)
            {
                UploadCustomFont(combo, owner);
            }
        };
    }

    private static void UploadCustomFont(ComboBox combo, IWin32Window owner)
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "Font files (*.ttf;*.otf)|*.ttf;*.otf",
            Title = "Choose a font file"
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            // Revert the combo back to whatever it was showing before, rather than leaving
            // the "Upload" placeholder selected as if it were a real font.
            PopulateFontCombo(combo, combo.Items.Count > 1 ? combo.Items[0].ToString() ?? "" : "");
            return;
        }

        string? installedFontName = CustomFontService.AddFontFromFile(dialog.FileName);

        if (installedFontName is null)
        {
            WadevoMessageBox.Show(
                owner,
                "That file couldn't be used as a font. Make sure it's a valid .ttf or .otf file.",
                "Font Upload Failed");

            PopulateFontCombo(combo, combo.Items.Count > 1 ? combo.Items[0].ToString() ?? "" : "");
            return;
        }

        PopulateFontCombo(combo, installedFontName);
    }
}
