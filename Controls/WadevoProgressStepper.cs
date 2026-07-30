namespace Wadevo.Controls;

using System.ComponentModel;
using System.Drawing.Drawing2D;
using Wadevo.Core;

public class WadevoProgressStepper : Control
{
    private int _currentStep;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] Steps { get; set; } = { "Type", "Details", "Options", "Preview" };

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            _currentStep = Math.Max(0, Math.Min(value, Steps.Length - 1));
            Invalidate();
        }
    }

    public WadevoProgressStepper()
    {
        Size = new Size(470, 74);
        MinimumSize = new Size(360, 74);
        DoubleBuffered = true;
        BackColor = WadevoTheme.Colors.Background;
    }

    private Color FindOpaqueBackColor()
    {
        Control? current = Parent;

        while (current is not null)
        {
            if (current.BackColor != Color.Transparent)
            {
                return current.BackColor;
            }

            current = current.Parent;
        }

        return WadevoTheme.Colors.Background;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(FindOpaqueBackColor());

        if (Steps.Length == 0)
            return;

        int circleSize = 30;
        int radius = circleSize / 2;
        int leftPadding = 48;
        int rightPadding = 48;
        int centerY = 25;
        int labelY = 50;

        int firstCenterX = leftPadding;
        int lastCenterX = Math.Max(firstCenterX, Width - rightPadding);

        int gap = Steps.Length == 1
            ? 0
            : (lastCenterX - firstCenterX) / (Steps.Length - 1);

        for (int i = 0; i < Steps.Length - 1; i++)
        {
            int x1 = firstCenterX + (gap * i) + radius + 4;
            int x2 = firstCenterX + (gap * (i + 1)) - radius - 4;

            using Pen linePen = new(i < CurrentStep ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.TextMuted, 3);
            linePen.StartCap = LineCap.Round;
            linePen.EndCap = LineCap.Round;

            e.Graphics.DrawLine(linePen, x1, centerY, x2, centerY);
        }

        for (int i = 0; i < Steps.Length; i++)
        {
            int centerX = firstCenterX + (gap * i);
            bool active = i == CurrentStep;
            bool complete = i < CurrentStep;

            Rectangle circle = new(
                centerX - radius,
                centerY - radius,
                circleSize,
                circleSize);

            Color fill = active || complete
                ? WadevoTheme.Colors.Accent
                : WadevoTheme.Colors.BackgroundSoft;

            Color border = active || complete
                ? WadevoTheme.Colors.Accent
                : WadevoTheme.Colors.TextMuted;

            if (active)
            {
                Rectangle glow = new(circle.X - 7, circle.Y - 7, circle.Width + 14, circle.Height + 14);

                using SolidBrush glowBrush = new(Color.FromArgb(50, WadevoTheme.Colors.Accent));
                e.Graphics.FillEllipse(glowBrush, glow);
            }

            using SolidBrush fillBrush = new(fill);
            e.Graphics.FillEllipse(fillBrush, circle);

            using Pen borderPen = new(border, active ? 3 : 2);
            e.Graphics.DrawEllipse(borderPen, circle);

            string number = (i + 1).ToString();

            using Font numberFont = new(Font.FontFamily, 10, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics,
                number,
                numberFont,
                circle,
                active || complete ? Color.Black : WadevoTheme.Colors.Text,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);

            Rectangle labelRect = new(centerX - 45, labelY, 90, 22);

            using Font labelFont = new(Font.FontFamily, 8, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics,
                Steps[i],
                labelFont,
                labelRect,
                active ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.TextMuted,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);
        }
    }
}