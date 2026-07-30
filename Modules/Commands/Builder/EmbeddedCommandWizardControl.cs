namespace Wadevo.Modules.Commands.Builder;

using Wadevo.Controls;
using Wadevo.Core;
using Wadevo.Models;

public class EmbeddedCommandWizardControl : UserControl
{
    private readonly BuilderState _state = new();

    private readonly Panel _headerPanel = new();
    private readonly Panel _footerPanel = new();

    private readonly WadevoProgressStepper _stepper = new();
    private readonly WadevoBadge _badge = new();

    private readonly Panel _pageViewport = new();
    private readonly Panel _pageHost = new();

    private PictureBox? _slideOverlay;
    private System.Windows.Forms.Timer? _slideTimer;
    private bool _isChangingPage;

    private readonly WadevoButton _backButton = new();
    private readonly WadevoButton _nextButton = new();
    private readonly WadevoButton _cancelButton = new();

    private readonly List<CommandBuilderPage> _pages = new();
    private readonly CommandModel? _editingCommand;

    private int _pageIndex;

    public event EventHandler<CommandModel>? CommandBuilt;
    public event EventHandler? Cancelled;

    public EmbeddedCommandWizardControl(CommandModel? commandToEdit = null)
    {
        _editingCommand = commandToEdit;

        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;
        Margin = new Padding(0);
        Padding = new Padding(0);

        LoadCommandIntoState();

        // Header: badge (top-right) + stepper (below it), fixed height, always spans the
        // full available width automatically via Dock=Top - no manual width calculation.
        _headerPanel.Dock = DockStyle.Top;
        _headerPanel.Height = 118;
        _headerPanel.BackColor = Color.Transparent;
        _headerPanel.Padding = new Padding(22, 4, 22, 0);

        _badge.Size = new Size(190, 38);

        _stepper.Location = new Point(0, 40);
        _stepper.Size = new Size(900, 70);
        _stepper.Steps = new[] { "Type", "Details", "Permissions", "Options", "Preview" };

        _headerPanel.Resize += (_, _) =>
        {
            _badge.Location = new Point(_headerPanel.ClientSize.Width - _headerPanel.Padding.Right - _badge.Width, 4);
        };

        _headerPanel.Controls.Add(_badge);
        _headerPanel.Controls.Add(_stepper);

        // Footer: Back/Next/Cancel, right-aligned, fixed height, always spans the full
        // available width automatically via Dock=Bottom.
        _footerPanel.Dock = DockStyle.Bottom;
        _footerPanel.Height = 66;
        _footerPanel.BackColor = Color.Transparent;
        _footerPanel.Padding = new Padding(22, 8, 22, 8);

        _backButton.ButtonText = "← Back";
        _backButton.Size = new Size(110, 42);
        _backButton.AccentColor = WadevoTheme.Colors.Cyan;
        _backButton.ButtonClicked += (_, _) => GoBack();

        _nextButton.ButtonText = "Next →";
        _nextButton.Size = new Size(145, 42);
        _nextButton.AccentColor = WadevoTheme.Colors.Accent;
        _nextButton.ButtonClicked += (_, _) => GoNext();

        _cancelButton.ButtonText = "Cancel";
        _cancelButton.Size = new Size(105, 42);
        _cancelButton.AccentColor = WadevoTheme.Colors.Purple;
        _cancelButton.ButtonClicked += (_, _) => Cancelled?.Invoke(this, EventArgs.Empty);

        void LayoutFooterButtons()
        {
            int right = _footerPanel.ClientSize.Width - _footerPanel.Padding.Right;
            int buttonTop = 8;

            _cancelButton.Location = new Point(right - _cancelButton.Width, buttonTop);
            _nextButton.Location = new Point(_cancelButton.Left - 15 - _nextButton.Width, buttonTop);
            _backButton.Location = new Point(_nextButton.Left - 15 - _backButton.Width, buttonTop);
        }

        _footerPanel.Resize += (_, _) => LayoutFooterButtons();
        LayoutFooterButtons();

        _footerPanel.Controls.Add(_backButton);
        _footerPanel.Controls.Add(_nextButton);
        _footerPanel.Controls.Add(_cancelButton);

        // The actual content area - Dock=Fill means it automatically occupies whatever
        // space is left between the header and footer, recalculated natively by the
        // layout engine on every resize. No manual "pageBottom - pageTop" math at all,
        // which is what kept proving unreliable across several previous attempts.
        _pageViewport.Dock = DockStyle.Fill;
        _pageViewport.BackColor = WadevoTheme.Colors.Background;
        _pageViewport.Margin = new Padding(0);
        _pageViewport.Padding = new Padding(22, 8, 22, 8);
        _pageViewport.AutoScroll = false;

        _pageHost.BackColor = WadevoTheme.Colors.Background;
        _pageHost.Margin = new Padding(0);
        _pageHost.Padding = new Padding(0);
        _pageHost.Location = new Point(0, 0);

        // Deliberately NOT Dock=Fill - the slide animation manually repositions this control
        // (_pageHost.Left) during the transition between steps, which Dock=Fill would
        // completely override (a docked control ignores manual Size/Location entirely).
        // Instead it's kept in sync with the now-reliable, natively-docked viewport size.
        _pageViewport.Resize += (_, _) =>
        {
            if (!_isChangingPage)
            {
                _pageHost.Size = _pageViewport.ClientSize;
            }
        };

        _pageViewport.Controls.Add(_pageHost);

        Controls.Add(_pageViewport);
        Controls.Add(_footerPanel);
        Controls.Add(_headerPanel);

        _pages.Add(new CommandTypePage());
        _pages.Add(new CommandDetailsPage());
        _pages.Add(new CommandPermissionsPage());
        _pages.Add(new CommandOptionsPage());
        _pages.Add(new CommandPreviewPage());

        ShowPage(0);

        // Safety net for the very first render - this control may not be mounted into its
        // true parent chain yet when the constructor runs (ShowPage(0) above can still see
        // WinForms' small construction-time default size at that exact moment). Load fires
        // once it's genuinely part of the visible tree with its real size.
        Load += (_, _) =>
        {
            _pageHost.Size = _pageViewport.ClientSize;

            if (_pageHost.Controls.Count > 0 && _pageHost.Controls[0] is CommandBuilderPage currentPage)
            {
                currentPage.OnHostResized();
            }
        };
    }

    private void LoadCommandIntoState()
    {
        if (_editingCommand is null)
            return;

        _state.CommandType = string.IsNullOrWhiteSpace(_editingCommand.CommandKind)
            ? "Chat Message"
            : _editingCommand.CommandKind == "GIF/Image"
                ? "GIF / Image"
                : _editingCommand.CommandKind;

        _state.CommandName = _editingCommand.Name;
        _state.ChatTriggers = _editingCommand.Trigger;
        _state.TriggerMode = string.IsNullOrWhiteSpace(_editingCommand.TriggerMode)
            ? "Chat Trigger"
            : _editingCommand.TriggerMode;
        _state.IntervalMinutes = _editingCommand.IntervalMinutes.ToString();
        _state.Output = IsMediaKind(_state.CommandType)
            ? _editingCommand.MediaFilePath
            : _editingCommand.Response;
        _state.RequirePrefix = _editingCommand.RequireExclamation;
        _state.EnableCommand = _editingCommand.IsEnabled;
        _state.ShowInQuickPanel = _editingCommand.ShowInQuickPanel;
        _state.CooldownSeconds = _editingCommand.CooldownSeconds.ToString();
        _state.MinimumRole = string.IsNullOrWhiteSpace(_editingCommand.MinimumRole)
            ? "Everyone"
            : _editingCommand.MinimumRole;
        _state.Width = _editingCommand.Width.ToString();
        _state.Height = _editingCommand.Height.ToString();
        _state.Duration = _editingCommand.DurationSeconds.ToString();
    }

    private void ShowPage(int index)
    {
        _pageIndex = index;
        RebuildPageContent();
    }

    private void RebuildPageContent()
    {
        CommandBuilderPage page = _pages[_pageIndex];
        page.LoadFromState(_state);
        page.Dock = DockStyle.Fill;
        page.Margin = new Padding(0);

        _pageHost.Controls.Clear();
        _pageHost.Controls.Add(page);

        // Dock=Fill (all the way down: viewport -> host -> page) handles sizing natively
        // now - no more manual "pageBottom - pageTop" calculation needed at all. Pages with
        // their own manually-positioned content (like the Multi Action list) still need an
        // explicit nudge to recalculate once they're actually at their real size, since
        // that can briefly lag behind the Dock=Fill assignment above.
        page.OnHostResized();

        _stepper.CurrentStep = _pageIndex;
        _badge.BadgeText = $"Building: {_state.CommandType}";

        _backButton.Visible = _pageIndex > 0;

        _nextButton.ButtonText = _pageIndex == _pages.Count - 1
            ? "Build Command"
            : "Next →";
    }

    private void GoBack()
    {
        _pages[_pageIndex].SaveToState(_state);

        if (_pageIndex <= 0 || _isChangingPage)
            return;

        AnimatePageChange(_pageIndex - 1, -1);
    }

    private void GoNext()
    {
        if (_isChangingPage)
            return;

        CommandBuilderPage currentPage = _pages[_pageIndex];

        if (!currentPage.CanMoveNext())
            return;

        currentPage.SaveToState(_state);

        if (_pageIndex == _pages.Count - 1)
        {
            SaveCommand();
            return;
        }

        AnimatePageChange(_pageIndex + 1, 1);
    }

    private void AnimatePageChange(int targetIndex, int direction)
    {
        _isChangingPage = true;
        _backButton.Enabled = false;
        _nextButton.Enabled = false;

        int viewportWidth = Math.Max(_pageViewport.ClientSize.Width, 1);
        int viewportHeight = Math.Max(_pageViewport.ClientSize.Height, 1);

        // Snapshot the outgoing page exactly as it looks right now, so it can slide away as one piece.
        // Pre-filled with the real theme background first - DrawToBitmap doesn't reliably
        // handle a control's Transparent BackColor (a known WinForms limitation), which was
        // producing a brief flash of the wrong color in the snapshot during the slide.
        Bitmap snapshot = new(viewportWidth, viewportHeight);

        using (Graphics snapshotGraphics = Graphics.FromImage(snapshot))
        {
            snapshotGraphics.Clear(WadevoTheme.Colors.Background);
        }

        _pageHost.DrawToBitmap(snapshot, new Rectangle(Point.Empty, new Size(viewportWidth, viewportHeight)));

        _slideOverlay?.Dispose();
        _slideOverlay = new PictureBox
        {
            Image = snapshot,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(viewportWidth, viewportHeight),
            Location = new Point(0, 0)
        };

        _pageViewport.Controls.Add(_slideOverlay);
        _slideOverlay.BringToFront();

        // Build the new page underneath the snapshot, positioned off to the side it should slide in from.
        _pageHost.Visible = false;
        _pageHost.SuspendLayout();

        _pageIndex = targetIndex;
        RebuildPageContent();

        _pageHost.Size = new Size(viewportWidth, viewportHeight);
        _pageHost.Location = new Point(viewportWidth * direction, 0);
        _pageHost.ResumeLayout();
        _pageHost.Visible = true;

        int elapsed = 0;
        const int durationMs = 260;
        const int intervalMs = 15;

        _slideTimer?.Stop();
        _slideTimer?.Dispose();
        _slideTimer = new System.Windows.Forms.Timer { Interval = intervalMs };

        _slideTimer.Tick += (_, _) =>
        {
            elapsed += intervalMs;
            double t = Math.Min(1.0, elapsed / (double)durationMs);
            double eased = 1 - Math.Pow(1 - t, 3);

            _pageHost.Left = (int)(viewportWidth * direction * (1 - eased));

            if (_slideOverlay is not null)
                _slideOverlay.Left = (int)(-viewportWidth * direction * eased);

            if (t >= 1.0)
            {
                _slideTimer?.Stop();
                _slideTimer?.Dispose();
                _slideTimer = null;

                _pageHost.Left = 0;

                if (_slideOverlay is not null)
                {
                    _pageViewport.Controls.Remove(_slideOverlay);
                    _slideOverlay.Image?.Dispose();
                    _slideOverlay.Dispose();
                    _slideOverlay = null;
                }

                _isChangingPage = false;
                _backButton.Enabled = _pageIndex > 0;
                _nextButton.Enabled = true;
            }
        };

        _slideTimer.Start();
    }

    private static bool IsMediaKind(string commandKind)
    {
        return commandKind is "GIF / Image" or "GIF/Image" or "Video Clip" or "Sound Effect";
    }

    private void SaveCommand()
    {
        bool isMedia = IsMediaKind(_state.CommandType);

        CommandModel command = new()
        {
            Name = _state.CommandName,
            Trigger = _state.ChatTriggers,
            TriggerMode = _state.TriggerMode,
            IntervalMinutes = ParsePositiveInt(_state.IntervalMinutes, 30),
            // A Timer-mode command being edited shouldn't have its progress toward the
            // next fire reset just because someone tweaked the message - only a brand
            // new command starts with no history.
            LastFiredAt = _editingCommand?.LastFiredAt,
            CommandKind = _state.CommandType,
            RequireExclamation = _state.RequirePrefix,
            IsEnabled = _state.EnableCommand,
            ShowInQuickPanel = _state.ShowInQuickPanel,
            CooldownSeconds = ParsePositiveInt(_state.CooldownSeconds, 0, allowZero: true),
            MinimumRole = string.IsNullOrWhiteSpace(_state.MinimumRole) ? "Everyone" : _state.MinimumRole,
            Response = isMedia ? "" : _state.Output,
            MediaFilePath = isMedia ? _state.Output : "",
            Width = ParsePositiveInt(_state.Width, 400),
            Height = ParsePositiveInt(_state.Height, 300),
            DurationSeconds = ParsePositiveInt(_state.Duration, 5)
        };

        CommandBuilt?.Invoke(this, command);
    }

    private static int ParsePositiveInt(string text, int fallback, bool allowZero = false)
    {
        if (!int.TryParse(text, out int value))
        {
            return fallback;
        }

        return allowZero ? Math.Max(0, value) : (value > 0 ? value : fallback);
    }
}
