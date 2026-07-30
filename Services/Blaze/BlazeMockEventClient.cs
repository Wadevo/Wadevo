namespace Wadevo.Services.Blaze;

public sealed class BlazeMockEventClient : IBlazeEventClient
{
    private CancellationTokenSource? _mockCancellation;
    private Task? _mockTask;

    public bool IsConnected { get; private set; }

    public event EventHandler<BlazeEvent>? EventReceived;

    public Task ConnectAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return Task.CompletedTask;

        IsConnected = true;

        _mockCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _mockTask = RunMockEventsAsync(_mockCancellation.Token);

        Dispatch(new BlazeEvent
        {
            EventType = BlazeEventType.Connected,
            Username = "Wadevo",
            Message = "Mock Blaze event client connected."
        });

        return Task.CompletedTask;
    }

    public async Task DisconnectAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            return;

        IsConnected = false;

        if (_mockCancellation is not null)
        {
            await _mockCancellation.CancelAsync();
            _mockCancellation.Dispose();
            _mockCancellation = null;
        }

        _mockTask = null;

        Dispatch(new BlazeEvent
        {
            EventType = BlazeEventType.Disconnected,
            Username = "Wadevo",
            Message = "Mock Blaze event client disconnected."
        });
    }

    private async Task RunMockEventsAsync(CancellationToken cancellationToken)
    {
        BlazeEvent[] events =
        [
            new()
            {
                EventType = BlazeEventType.ChatMessage,
                Username = "AtlasTester",
                MessageId = Guid.NewGuid().ToString(),
                Message = "Hello from mock Blaze chat!"
            },
            new()
            {
                EventType = BlazeEventType.Follow,
                Username = "NewFollower",
                Message = "NewFollower followed the channel."
            },
            new()
            {
                EventType = BlazeEventType.Raid,
                Username = "RaidLeader",
                Message = "RaidLeader raided with 12 viewers.",
                Data = new Dictionary<string, object?>
                {
                    ["viewerCount"] = 12
                }
            }
        ];

        int index = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                break;

            Dispatch(events[index]);

            index++;

            if (index >= events.Length)
                index = 0;
        }
    }

    private void Dispatch(BlazeEvent blazeEvent)
    {
        EventReceived?.Invoke(this, blazeEvent);
    }
}
