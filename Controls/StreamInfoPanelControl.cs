namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Models;
using Wadevo.Services;
using Wadevo.Services.Blaze;

/// <summary>
/// A compact version of the Dashboard's Stream Info block, sized to fit a Workspace
/// Studio panel - so the title can be updated from a pop-out window without needing to
/// switch to the Dashboard tab at all.
/// </summary>
public sealed class StreamInfoPanelControl : UserControl
{
    private static readonly string[] KnownDirectoryNames =
        { "Game", "IRL", "Music", "Art", "Crypto", "Gambling & Casino" };

    private readonly BlazeChannelService _blazeChannelService = new();
    private readonly BlazeCategoryService _blazeCategoryService = new();
    private readonly Dictionary<string, int> _categoryNameToId = new(StringComparer.OrdinalIgnoreCase);
    private int? _gameDirectoryId;
    private readonly Dictionary<string, int> _subCategoryNameToId = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _categoryRetryTimer = new() { Interval = 30_000 };
    private bool _categoriesLoaded;

    private readonly WadevoTextBox _titleTextBox = new();
    private readonly WadevoComboBox _categoryComboBox = new();
    private readonly Label _subCategoryLabel = new();
    private readonly WadevoTextBox _gameSearchTextBox = new();
    private readonly WadevoButton _searchGameButton = new();
    private readonly WadevoComboBox _gameResultsComboBox = new();
    private readonly WadevoComboBox _languageTextBox = new();
    private readonly WadevoButton _updateButton = new();
    private readonly Label _statusLabel = new();

    public StreamInfoPanelControl()
    {
        Dock = DockStyle.Fill;
        AutoScroll = true;
        BackColor = Color.Transparent;

        _titleTextBox.PlaceholderText = "Enter your stream title…";
        _titleTextBox.Location = new Point(8, 8);
        _titleTextBox.Size = new Size(300, 34);

        Label categoryLabel = new()
        {
            Text = "Directory",
            Location = new Point(8, 50),
            Size = new Size(150, 16),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _categoryComboBox.Location = new Point(8, 68);
        _categoryComboBox.Size = new Size(300, 32);
        _categoryComboBox.Font = WadevoTheme.Fonts.Default;
        _categoryComboBox.ForeColor = WadevoTheme.Colors.Text;
        _categoryComboBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _categoryComboBox.FlatStyle = FlatStyle.Flat;
        _categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryComboBox.TextAlign = ContentAlignment.MiddleCenter;
        _categoryComboBox.Items.Add("Connect Blaze…");
        _categoryComboBox.SelectedIndex = 0;
        _categoryComboBox.SelectedIndexChanged += (_, _) => ApplyDirectorySelection();

        Label languageLabel = new()
        {
            Text = "Language",
            Location = new Point(8, 108),
            Size = new Size(150, 16),
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent
        };

        _languageTextBox.Items.AddRange(BlazeLanguageOptions.Common.Select(l => (object)l.Code).ToArray());
        _languageTextBox.Text = "en";
        _languageTextBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageTextBox.TextAlign = ContentAlignment.MiddleCenter;
        // Its own full-width row now, not squeezed to 46px beside Directory - that was
        // nowhere near enough room to show even a short language code without truncating.
        _languageTextBox.Location = new Point(8, 126);
        _languageTextBox.Size = new Size(140, 32);
        _languageTextBox.Font = WadevoTheme.Fonts.Default;
        _languageTextBox.ForeColor = WadevoTheme.Colors.Text;
        _languageTextBox.BackColor = WadevoTheme.Colors.BackgroundSoft;

        // Only the Game directory drills into a further search, matching Blaze's own
        // "Edit Stream Info" dialog - it's a live search box there, not a pre-loaded list.
        _subCategoryLabel.Text = "Game (search by name)";
        _subCategoryLabel.Location = new Point(8, 166);
        _subCategoryLabel.Size = new Size(300, 16);
        _subCategoryLabel.Font = WadevoTheme.Fonts.Small;
        _subCategoryLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _subCategoryLabel.BackColor = Color.Transparent;
        _subCategoryLabel.Visible = false;

        // A plain typing box (not an editable combo) - a standalone TextBox reliably
        // supports centered text; an editable combo's text area is a native control that
        // doesn't take styling changes applied after creation.
        _gameSearchTextBox.PlaceholderText = "Type a game name…";
        _gameSearchTextBox.Location = new Point(8, 184);
        _gameSearchTextBox.Size = new Size(300, 34);
        _gameSearchTextBox.TextAlign = HorizontalAlignment.Center;
        _gameSearchTextBox.Visible = false;

        _searchGameButton.ButtonText = "🔍 Search";
        _searchGameButton.Location = new Point(8, 222);
        _searchGameButton.Size = new Size(300, 32);
        _searchGameButton.AccentColor = WadevoTheme.Colors.Cyan;
        _searchGameButton.Visible = false;
        _searchGameButton.ButtonClicked += (_, _) => _ = SearchGamesAsync();

        _gameSearchTextBox.InnerKeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = SearchGamesAsync();
            }
        };

        // Results picker - a DropDownList, same reliable owner-drawn approach as
        // Directory, shown once a search actually returns matches to choose from.
        _gameResultsComboBox.Location = new Point(8, 260);
        _gameResultsComboBox.Size = new Size(300, 32);
        _gameResultsComboBox.Font = WadevoTheme.Fonts.Default;
        _gameResultsComboBox.ForeColor = WadevoTheme.Colors.Text;
        _gameResultsComboBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _gameResultsComboBox.FlatStyle = FlatStyle.Flat;
        _gameResultsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _gameResultsComboBox.TextAlign = ContentAlignment.MiddleCenter;
        _gameResultsComboBox.Visible = false;

        _updateButton.ButtonText = "Update";
        _updateButton.Location = new Point(8, 300);
        _updateButton.Size = new Size(300, 36);
        _updateButton.ButtonClicked += (_, _) => _ = UpdateStreamInfoAsync();

        _statusLabel.Location = new Point(8, 342);
        _statusLabel.Size = new Size(300, 40);
        _statusLabel.Font = WadevoTheme.Fonts.Small;
        _statusLabel.ForeColor = WadevoTheme.Colors.TextMuted;
        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.Text = "";

        Controls.Add(_titleTextBox);
        Controls.Add(categoryLabel);
        Controls.Add(_categoryComboBox);
        Controls.Add(languageLabel);
        Controls.Add(_languageTextBox);
        Controls.Add(_subCategoryLabel);
        Controls.Add(_gameSearchTextBox);
        Controls.Add(_searchGameButton);
        Controls.Add(_gameResultsComboBox);
        Controls.Add(_updateButton);
        Controls.Add(_statusLabel);

        HandleCreated += (_, _) => _ = LoadCategoriesAsync();

        // If Blaze wasn't connected yet when this panel opened, the load above fails
        // silently - this retries periodically so connecting later (without needing to
        // close and reopen the panel) still picks up the directory list.
        _categoryRetryTimer.Tick += (_, _) =>
        {
            if (!_categoriesLoaded)
            {
                _ = LoadCategoriesAsync();
            }
        };
        _categoryRetryTimer.Start();

        Disposed += (_, _) => _categoryRetryTimer.Dispose();
    }

    private async Task LoadCategoriesAsync()
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            return;
        }

        try
        {
            _categoryNameToId.Clear();
            _categoryComboBox.Items.Clear();
            _gameDirectoryId = null;

            foreach (string name in KnownDirectoryNames)
            {
                string[] termsToTry = name == "Game" ? new[] { "Game", "Games", "Gaming" } : new[] { name };
                BlazeCategoryModel? match = null;

                foreach (string term in termsToTry)
                {
                    IReadOnlyList<BlazeCategoryModel> results = await _blazeCategoryService.SearchCategoriesAsync(
                        auth.Connection.AccessToken,
                        auth.Settings.ClientId,
                        auth.Settings.ClientSecret,
                        term);

                    match =
                        results.FirstOrDefault(c => string.Equals(c.Name, term, StringComparison.OrdinalIgnoreCase))
                        ?? results.FirstOrDefault(c => c.ParentId is null &&
                            c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

                    if (match is not null)
                    {
                        break;
                    }
                }

                if (match is not null && _categoryNameToId.TryAdd(match.Name, match.Id))
                {
                    _categoryComboBox.Items.Add(match.Name);

                    if (name == "Game")
                    {
                        _gameDirectoryId = match.Id;
                    }
                }
            }

            // Data-driven last resort: find whichever parentId is referenced by the most
            // children in a broad sample, rather than continuing to guess Game's actual
            // name - see the Dashboard's LoadCategoriesAsync for the full reasoning.
            if (_gameDirectoryId is null)
            {
                try
                {
                    IReadOnlyList<BlazeCategoryModel> broadSample = await _blazeCategoryService.GetCategoriesAsync(
                        auth.Connection.AccessToken,
                        auth.Settings.ClientId,
                        auth.Settings.ClientSecret);

                    var mostCommonParentId = broadSample
                        .Where(c => c.ParentId is not null)
                        .GroupBy(c => c.ParentId!.Value)
                        .OrderByDescending(group => group.Count())
                        .Select(group => (Id: group.Key, Count: group.Count()))
                        .FirstOrDefault();

                    if (mostCommonParentId.Count > 0)
                    {
                        BlazeCategoryModel? directoryRow = broadSample.FirstOrDefault(c => c.Id == mostCommonParentId.Id);
                        string directoryName = directoryRow?.Name ?? "Game";

                        if (_categoryNameToId.TryAdd(directoryName, mostCommonParentId.Id))
                        {
                            _categoryComboBox.Items.Add(directoryName);
                        }

                        _gameDirectoryId = mostCommonParentId.Id;
                    }
                }
                catch (Exception ex)
                {
                    WadevoLogger.Warning($"Game directory fallback lookup failed: {ex.Message}");
                }
            }

            if (IsDisposed)
            {
                return;
            }

            _categoriesLoaded = true;

            if (_categoryComboBox.Items.Count > 0)
            {
                _categoryComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Stream info panel category list failed to load: {ex.Message}");
        }
    }

    private void ApplyDirectorySelection()
    {
        bool isGameDirectory =
            _gameDirectoryId is not null &&
            _categoryNameToId.TryGetValue(_categoryComboBox.Text.Trim(), out int selectedId) &&
            selectedId == _gameDirectoryId;

        _subCategoryLabel.Visible = isGameDirectory;
        _gameSearchTextBox.Visible = isGameDirectory;
        _searchGameButton.Visible = isGameDirectory;
        _gameSearchTextBox.Text = "";
        _gameResultsComboBox.Visible = false;
        _gameResultsComboBox.Items.Clear();
        _subCategoryNameToId.Clear();
    }

    private async Task SearchGamesAsync()
    {
        string term = _gameSearchTextBox.Text.Trim();

        if (term.Length < 2)
        {
            return;
        }

        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            return;
        }

        try
        {
            IReadOnlyList<BlazeCategoryModel> results = await _blazeCategoryService.SearchCategoriesAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                term);

            if (IsDisposed)
            {
                return;
            }

            _subCategoryNameToId.Clear();
            _gameResultsComboBox.Items.Clear();

            foreach (BlazeCategoryModel result in results.Where(r => r.ParentId is not null))
            {
                if (_subCategoryNameToId.TryAdd(result.Name, result.Id))
                {
                    _gameResultsComboBox.Items.Add(result.Name);
                }
            }

            _gameResultsComboBox.Visible = _gameResultsComboBox.Items.Count > 0;

            if (_gameResultsComboBox.Items.Count > 0)
            {
                _gameResultsComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            WadevoLogger.Warning($"Stream info panel game search failed: {ex.Message}");
        }
    }

    private async Task UpdateStreamInfoAsync()
    {
        BlazeAuthenticationService auth = BlazeAuthenticationService.Shared;

        if (!auth.IsAuthenticated)
        {
            _statusLabel.Text = "Connect Blaze first (Connections tab).";
            _statusLabel.ForeColor = WadevoTheme.Colors.Error;
            return;
        }

        string title = _titleTextBox.Text.Trim();
        string lang = string.IsNullOrWhiteSpace(_languageTextBox.Text) ? "en" : _languageTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            _statusLabel.Text = "Title can't be empty.";
            _statusLabel.ForeColor = WadevoTheme.Colors.Error;
            return;
        }

        int? categoryId = _gameResultsComboBox.Visible
            ? (_subCategoryNameToId.TryGetValue(_gameResultsComboBox.Text.Trim(), out int subId) ? subId : null)
            : (_categoryNameToId.TryGetValue(_categoryComboBox.Text.Trim(), out int id) ? id : null);

        _updateButton.Enabled = false;
        _statusLabel.Text = "Updating…";
        _statusLabel.ForeColor = WadevoTheme.Colors.Warning;

        try
        {
            await _blazeChannelService.UpdateChannelInfoAsync(
                auth.Connection.AccessToken,
                auth.Settings.ClientId,
                auth.Settings.ClientSecret,
                title,
                lang,
                categoryId);

            if (IsDisposed)
            {
                return;
            }

            _statusLabel.Text = "✓ Updated.";
            _statusLabel.ForeColor = WadevoTheme.Colors.Success;
        }
        catch (Exception ex)
        {
            WadevoLogger.Error("Stream info panel update failed", ex);

            if (!IsDisposed)
            {
                _statusLabel.Text = $"Couldn't update: {ex.Message.Split("\r\n")[0]}";
                _statusLabel.ForeColor = WadevoTheme.Colors.Error;
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _updateButton.Enabled = true;
            }
        }
    }
}
