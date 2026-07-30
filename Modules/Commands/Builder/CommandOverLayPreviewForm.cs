namespace Wadevo.Modules.Commands.Builder;

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Wadevo.Core;

public class CommandOverlayPreviewForm : Form
{
    private readonly BuilderState _state;
    private readonly System.Windows.Forms.Timer _closeTimer = new();
    private readonly System.Windows.Forms.Timer _fadeTimer = new();

    private Image? _loadedImage;
    private PictureBox? _pictureBox;
    private int _fadeDirection = 1;

    public CommandOverlayPreviewForm(BuilderState state)
    {
        _state = state;

        Text = "Wadevo Test Preview";
        Size = GetPreviewSize();
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = WadevoTheme.Colors.Background;
        Font = WadevoTheme.Fonts.Default;
        DoubleBuffered = true;
        Opacity = 0;

        BuildContent();

        _fadeTimer.Interval = 20;
        _fadeTimer.Tick += (_, _) => FadeTick();

        _closeTimer.Interval = GetDurationMilliseconds();
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            _fadeDirection = -1;
            _fadeTimer.Start();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        StartAnimatedImageIfNeeded();

        _fadeDirection = 1;
        _fadeTimer.Start();
        _closeTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        StopAnimatedImageIfNeeded();

        _loadedImage?.Dispose();
        _loadedImage = null;

        base.OnFormClosed(e);
    }

    private void BuildContent()
    {
        Controls.Clear();

        if (_state.CommandType == "GIF / Image" && File.Exists(_state.Output))
        {
            BuildMediaPreview();
            return;
        }

        if ((_state.CommandType == "Video Clip" || _state.CommandType == "Sound Effect")
            && File.Exists(_state.Output))
        {
            TryPlayMediaExternally(_state.Output);
        }

        BuildCommandPreview();
    }

    private static void TryPlayMediaExternally(string path)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
            // Test preview should never crash the app if the system has no player
            // associated with this file type - the command info still shows either way.
        }
    }

    private void BuildMediaPreview()
    {
        _loadedImage = Image.FromFile(_state.Output);

        Panel frame = CreatePreviewFrame();
        frame.Dock = DockStyle.Fill;
        frame.Padding = new Padding(18);

        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Image = _loadedImage
        };

        frame.Controls.Add(_pictureBox);
        Controls.Add(frame);
    }

    private void BuildCommandPreview()
    {
        Panel frame = CreatePreviewFrame();
        frame.Dock = DockStyle.Fill;
        frame.Padding = new Padding(26);

        Label title = new()
        {
            Text = GetPreviewTitle(),
            Dock = DockStyle.Top,
            Height = 38,
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Accent,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label trigger = new()
        {
            Text = GetTriggerText(),
            Dock = DockStyle.Top,
            Height = 34,
            Font = WadevoTheme.Fonts.Bold,
            ForeColor = WadevoTheme.Colors.Cyan,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label outputHeader = new()
        {
            Text = "Output Preview",
            Dock = DockStyle.Top,
            Height = 28,
            Font = WadevoTheme.Fonts.Medium,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomLeft
        };

        Label output = new()
        {
            Text = GetPreviewText(),
            Dock = DockStyle.Fill,
            Font = WadevoTheme.Fonts.CardHeader,
            ForeColor = WadevoTheme.Colors.Text,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Label footer = new()
        {
            Text = "This is a test preview only. No command was sent.",
            Dock = DockStyle.Bottom,
            Height = 28,
            Font = WadevoTheme.Fonts.Small,
            ForeColor = WadevoTheme.Colors.TextMuted,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        frame.Controls.Add(output);
        frame.Controls.Add(footer);
        frame.Controls.Add(outputHeader);
        frame.Controls.Add(trigger);
        frame.Controls.Add(title);

        Controls.Add(frame);
    }

    private static Panel CreatePreviewFrame()
    {
        return new PreviewFramePanel
        {
            BackColor = WadevoTheme.Colors.Card,
            Margin = Padding.Empty
        };
    }

    private void StartAnimatedImageIfNeeded()
    {
        if (_loadedImage is null || _pictureBox is null || !ImageAnimator.CanAnimate(_loadedImage))
            return;

        ImageAnimator.Animate(_loadedImage, (_, _) =>
        {
            if (_pictureBox.IsDisposed)
                return;

            _pictureBox.Invalidate();
        });

        _pictureBox.Paint += PictureBoxPaint;
    }

    private void StopAnimatedImageIfNeeded()
    {
        if (_loadedImage is null || !ImageAnimator.CanAnimate(_loadedImage))
            return;

        ImageAnimator.StopAnimate(_loadedImage, null);

        if (_pictureBox is not null)
            _pictureBox.Paint -= PictureBoxPaint;
    }

    private void PictureBoxPaint(object? sender, PaintEventArgs e)
    {
        if (_loadedImage is null)
            return;

        ImageAnimator.UpdateFrames(_loadedImage);
    }

    private Size GetPreviewSize()
    {
        if (_state.CommandType == "GIF / Image" && File.Exists(_state.Output))
        {
            int width = ParseNumber(_state.Width, 520);
            int height = ParseNumber(_state.Height, 320);

            width = Math.Clamp(width, 320, 1200);
            height = Math.Clamp(height, 220, 800);

            return new Size(width, height);
        }

        return new Size(620, 330);
    }

    private int GetDurationMilliseconds()
    {
        int seconds = ParseNumber(_state.Duration, 6);
        seconds = Math.Clamp(seconds, 2, 30);

        return seconds * 1000;
    }

    private string GetPreviewTitle()
    {
        string commandName = string.IsNullOrWhiteSpace(_state.CommandName)
            ? "Untitled Command"
            : _state.CommandName.Trim();

        return $"{GetIcon(_state.CommandType)}  {commandName}";
    }

    private string GetTriggerText()
    {
        string trigger = string.IsNullOrWhiteSpace(_state.ChatTriggers)
            ? "No trigger set"
            : _state.ChatTriggers.Trim();

        return $"Trigger: {trigger}";
    }

    private string GetPreviewText()
    {
        string output = string.IsNullOrWhiteSpace(_state.Output)
            ? "Wadevo Preview"
            : _state.Output.Trim();

        return _state.CommandType switch
        {
            "Chat Message" => $"Chat Message:\n{output}",
            "Alert" => $"🔔 On-Stream Alert:\n{output}",
            "Video Clip" => $"Video Clip Preview\n{Path.GetFileName(output)}",
            "Sound Effect" => $"Sound Effect Preview\n{Path.GetFileName(output)}",
            "Multi Action" => BuildMultiActionPreviewText(output),
            _ => output
        };
    }

    private static string BuildMultiActionPreviewText(string output)
    {
        string[] actions = output.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (actions.Length == 0)
        {
            return "Multi Action Preview\n(No actions added yet)";
        }

        return "Multi Action Preview:\n" + string.Join("\n", actions.Select((action, i) => $"{i + 1}. {action}"));
    }

    private void FadeTick()
    {
        Opacity += _fadeDirection * 0.08;

        if (_fadeDirection > 0 && Opacity >= 1)
        {
            Opacity = 1;
            _fadeTimer.Stop();
        }

        if (_fadeDirection < 0 && Opacity <= 0)
        {
            Opacity = 0;
            _fadeTimer.Stop();
            Close();
        }
    }

    private static string GetIcon(string commandType)
    {
        return commandType switch
        {
            "Chat Message" => "💬",
            "Alert" => "🔔",
            "GIF / Image" => "🖼",
            "Video Clip" => "🎬",
            "Sound Effect" => "🔊",
            "Multi Action" => "🎉",
            _ => "⭐"
        };
    }

    private static int ParseNumber(string value, int fallback)
    {
        return int.TryParse(value, out int result)
            ? result
            : fallback;
    }

    private sealed class PreviewFramePanel : Panel
    {
        public PreviewFramePanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using GraphicsPath path = RoundedRectangle(rect, 22);
            using SolidBrush fill = new(WadevoTheme.Colors.Card);
            using Pen border = new(WadevoTheme.Colors.Accent, 2);

            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new();

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
