namespace Wadevo.Controls;

using Wadevo.Core;
using Wadevo.Services.Blaze;

public sealed class LiveChatFeedPanelControl : UserControl
{
    private const int MaxMessages = 40;

    private readonly ListBox _messageListBox = new();

    public LiveChatFeedPanelControl()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        _messageListBox.Dock = DockStyle.Fill;
        _messageListBox.Font = WadevoTheme.Fonts.Small;
        _messageListBox.ForeColor = WadevoTheme.Colors.Text;
        _messageListBox.BackColor = WadevoTheme.Colors.BackgroundSoft;
        _messageListBox.BorderStyle = BorderStyle.FixedSingle;

        Controls.Add(_messageListBox);

        BlazeLiveEventService.Shared.EventReceived += LiveEventService_EventReceived;
    }

    private void LiveEventService_EventReceived(object? sender, BlazeEvent blazeEvent)
    {
        if (blazeEvent.EventType != BlazeEventType.ChatMessage)
        {
            return;
        }

        string line = $"{blazeEvent.Username ?? "Someone"}: {blazeEvent.Message}";

        // BeginInvoke throws if called before the control's window handle exists yet - a
        // real possibility for a panel restored from a saved workspace layout, where a
        // chat event could arrive in the brief window between construction and the
        // control actually being realized in the visual tree. Silently skipping that one
        // message is far better than an unhandled exception breaking this subscription
        // for every message after it.
        if (!IsHandleCreated)
        {
            return;
        }

        try
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => AddLine(line)));
            }
            else
            {
                AddLine(line);
            }
        }
        catch (InvalidOperationException)
        {
            // Control was disposed or its handle went away between the check above and
            // this call - safe to ignore, there's nothing left to update.
        }
    }

    private void AddLine(string line)
    {
        _messageListBox.Items.Insert(0, line);

        while (_messageListBox.Items.Count > MaxMessages)
        {
            _messageListBox.Items.RemoveAt(_messageListBox.Items.Count - 1);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BlazeLiveEventService.Shared.EventReceived -= LiveEventService_EventReceived;
        }

        base.Dispose(disposing);
    }
}
