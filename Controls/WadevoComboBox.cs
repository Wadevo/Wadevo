namespace Wadevo.Controls;

using System.Runtime.InteropServices;
using Wadevo.Core;

/// <summary>
/// A drop-in replacement for the plain WinForms ComboBox. Setting BackColor/ForeColor on a
/// stock ComboBox only affects the closed display box - the dropdown LIST is always
/// natively rendered in white regardless, since WinForms doesn't theme it without owner
/// drawing. This inherits from ComboBox (not a wrapper), so SelectedIndex, SelectedItem,
/// Items, DropDownStyle, and everything else callers already rely on keeps working
/// unchanged - only the item painting is overridden.
///
/// Deliberately NOT rounding the control's own corners (e.g. via Region shaping, which
/// works cleanly for Forms elsewhere in the app). ComboBox has its own complex native
/// rendering for the dropdown arrow and edit area that doesn't clip cleanly with a simple
/// Region - an earlier attempt at this made the control look visually broken rather than
/// rounded. Square corners here are the safer trade-off.
/// </summary>
public class WadevoComboBox : ComboBox
{
    private const int ComboBoxEditControlId = 1001;
    private const int GwlStyle = -16;
    private const int EsCenter = 0x0001;
    private const int EsRight = 0x0002;
    private const int EsAlignMask = 0x0003;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    private ContentAlignment _textAlign = ContentAlignment.MiddleLeft;

    // For a DropDownList-style combo this affects the closed-box display (handled in
    // OnDrawItem below, ordinary owner-draw). For an editable DropDown-style combo - where
    // the visible text box is a real native Win32 edit control, not something .NET owner-
    // draws - no managed property can reach it at all, so this reaches the actual native
    // control via P/Invoke and sets its ES_CENTER/ES_RIGHT style directly instead.
    public ContentAlignment TextAlign
    {
        get => _textAlign;
        set
        {
            _textAlign = value;
            ApplyNativeEditAlignment();
        }
    }

    public WadevoComboBox()
    {
        FlatStyle = FlatStyle.Flat;
        BackColor = WadevoTheme.Colors.BackgroundSoft;
        ForeColor = WadevoTheme.Colors.Text;
        Font = WadevoTheme.Fonts.Default;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 26;
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count)
        {
            using SolidBrush emptyBrush = new(WadevoTheme.Colors.BackgroundSoft);
            e.Graphics.FillRectangle(emptyBrush, e.Bounds);
            return;
        }

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        Color backColor = isSelected ? WadevoTheme.Colors.Accent : WadevoTheme.Colors.BackgroundSoft;
        Color foreColor = isSelected ? WadevoTheme.Colors.Background : WadevoTheme.Colors.Text;

        using (SolidBrush backBrush = new(backColor))
        {
            e.Graphics.FillRectangle(backBrush, e.Bounds);
        }

        string text = GetItemText(Items[e.Index]);

        TextFormatFlags alignFlag = TextAlign switch
        {
            ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter,
            ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => TextFormatFlags.Right,
            _ => TextFormatFlags.Left
        };

        TextRenderer.DrawText(
            e.Graphics,
            text,
            e.Font ?? Font,
            e.Bounds,
            foreColor,
            alignFlag | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        base.OnDrawItem(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyNativeEditAlignment();
    }

    private void ApplyNativeEditAlignment()
    {
        if (DropDownStyle != ComboBoxStyle.DropDown || !IsHandleCreated)
        {
            return;
        }

        // A standard ComboBox's editable text area is a child Edit control at the
        // well-known control id 1001 - this is a long-established Win32 convention, not
        // something specific to this app.
        IntPtr editHandle = GetDlgItem(Handle, ComboBoxEditControlId);

        if (editHandle == IntPtr.Zero)
        {
            return;
        }

        int style = GetWindowLong(editHandle, GwlStyle);

        int alignmentBits = _textAlign switch
        {
            ContentAlignment.MiddleCenter or ContentAlignment.TopCenter or ContentAlignment.BottomCenter => EsCenter,
            ContentAlignment.MiddleRight or ContentAlignment.TopRight or ContentAlignment.BottomRight => EsRight,
            _ => 0
        };

        style = (style & ~EsAlignMask) | alignmentBits;

        SetWindowLong(editHandle, GwlStyle, style);

        // Single-line edit controls are known to sometimes not re-evaluate their text
        // layout just because ES_CENTER/ES_RIGHT was set via SetWindowLong after the
        // control already existed (as opposed to being set at creation time) - explicitly
        // telling Windows the frame/style changed forces it to actually reprocess that,
        // rather than continuing to render with whatever it cached when first created.
        SetWindowPos(editHandle, IntPtr.Zero, 0, 0, 0, 0,
            SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);

        // Windows also doesn't re-center the native edit child when its parent ComboBox is
        // made taller than the system default (ours are, for the larger touch-friendly
        // look used throughout this app) - it just keeps its original small height,
        // pinned near the top. This repositions it to sit vertically centered instead,
        // keeping its existing X/width/height, only moving Y.
        if (GetWindowRect(editHandle, out RECT editScreenRect) && GetWindowRect(Handle, out RECT comboScreenRect))
        {
            int editWidth = editScreenRect.Right - editScreenRect.Left;
            int editHeight = editScreenRect.Bottom - editScreenRect.Top;
            int editX = editScreenRect.Left - comboScreenRect.Left;
            int centeredY = Math.Max(1, (Height - editHeight) / 2);

            MoveWindow(editHandle, editX, centeredY, editWidth, editHeight, true);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
