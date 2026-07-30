namespace Wadevo.Services.Obs;

using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Implements the obs-websocket v5 protocol (built into OBS Studio 28+, no plugin needed).
// This is a pure remote-control connection to the user's own local OBS - it has nothing to
// do with multistreaming to other platforms; it just lets Wadevo see and control whatever
// OBS is already doing (current scene, streaming status, start/stop, scene switches).
public sealed class ObsWebSocketClient : IAsyncDisposable
{
    // OpCodes from the obs-websocket v5 protocol.
    private const int OpHello = 0;
    private const int OpIdentify = 1;
    private const int OpIdentified = 2;
    private const int OpEvent = 5;
    private const int OpRequest = 6;
    private const int OpRequestResponse = 7;

    private const int RpcVersion = 1;

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _receiveLoopCancellation;
    private Task? _receiveLoopTask;

    private readonly Dictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly object _pendingRequestsLock = new();

    public bool IsConnected { get; private set; }

    public event EventHandler<(string EventType, JsonElement EventData)>? EventReceived;

    public event EventHandler? Disconnected;

    public async Task ConnectAsync(
        string host,
        int port,
        string password,
        CancellationToken cancellationToken = default)
    {
        _socket = new ClientWebSocket();

        Uri uri = new($"ws://{host}:{port}");
        await _socket.ConnectAsync(uri, cancellationToken);

        _receiveLoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // The Hello message arrives as the very first frame - read it directly here rather
        // than through the general receive loop, since the handshake has to complete
        // (Identify -> Identified) before it makes sense to start dispatching events/requests.
        string helloJson = await ReceiveOneMessageAsync(_receiveLoopCancellation.Token);
        using JsonDocument helloDocument = JsonDocument.Parse(helloJson);
        JsonElement helloData = helloDocument.RootElement.GetProperty("d");

        string authString = "";

        if (helloData.TryGetProperty("authentication", out JsonElement authInfo))
        {
            string challenge = authInfo.GetProperty("challenge").GetString() ?? "";
            string salt = authInfo.GetProperty("salt").GetString() ?? "";
            authString = ComputeAuthString(password, salt, challenge);
        }

        var identifyPayload = new Dictionary<string, object?>
        {
            ["op"] = OpIdentify,
            ["d"] = new Dictionary<string, object?>
            {
                ["rpcVersion"] = RpcVersion,
                ["authentication"] = string.IsNullOrEmpty(authString) ? null : authString,
                ["eventSubscriptions"] = 33 // General (1) + Outputs (32) - covers stream start/stop and scene changes without subscribing to noisy input-level events.
            }
        };

        await SendRawAsync(identifyPayload, _receiveLoopCancellation.Token);

        string identifiedJson = await ReceiveOneMessageAsync(_receiveLoopCancellation.Token);
        using JsonDocument identifiedDocument = JsonDocument.Parse(identifiedJson);
        int op = identifiedDocument.RootElement.GetProperty("op").GetInt32();

        if (op != OpIdentified)
        {
            throw new InvalidOperationException(
                "OBS rejected the connection - check the WebSocket password in OBS's Tools > WebSocket Server Settings.");
        }

        IsConnected = true;

        _receiveLoopTask = ReceiveLoopAsync(_receiveLoopCancellation.Token);
    }

    public async Task DisconnectAsync()
    {
        IsConnected = false;

        _receiveLoopCancellation?.Cancel();

        if (_socket is { State: WebSocketState.Open })
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
            }
            catch
            {
                // Best-effort close.
            }
        }

        if (_receiveLoopTask is not null)
        {
            try
            {
                await _receiveLoopTask;
            }
            catch
            {
                // Expected once cancelled.
            }
        }

        _socket?.Dispose();
        _socket = null;

        lock (_pendingRequestsLock)
        {
            foreach (TaskCompletionSource<JsonElement> pending in _pendingRequests.Values)
            {
                pending.TrySetCanceled();
            }

            _pendingRequests.Clear();
        }
    }

    // Sends an obs-websocket Request and waits for its matching RequestResponse. Throws if
    // OBS reports the request failed (e.g. scene name doesn't exist).
    public async Task<JsonElement> SendRequestAsync(
        string requestType,
        object? requestData = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _socket is null)
        {
            throw new InvalidOperationException("Not connected to OBS.");
        }

        string requestId = Guid.NewGuid().ToString("N");
        TaskCompletionSource<JsonElement> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_pendingRequestsLock)
        {
            _pendingRequests[requestId] = completionSource;
        }

        var payload = new Dictionary<string, object?>
        {
            ["op"] = OpRequest,
            ["d"] = new Dictionary<string, object?>
            {
                ["requestType"] = requestType,
                ["requestId"] = requestId,
                ["requestData"] = requestData
            }
        };

        await SendRawAsync(payload, cancellationToken);

        using CancellationTokenRegistration registration = cancellationToken.Register(() => completionSource.TrySetCanceled());

        return await completionSource.Task;
    }

    private static string ComputeAuthString(string password, string salt, string challenge)
    {
        using SHA256 sha256 = SHA256.Create();

        byte[] passwordSaltHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        string base64Secret = Convert.ToBase64String(passwordSaltHash);

        byte[] authHash = sha256.ComputeHash(Encoding.UTF8.GetBytes(base64Secret + challenge));
        return Convert.ToBase64String(authHash);
    }

    private async Task SendRawAsync(object payload, CancellationToken cancellationToken)
    {
        if (_socket is null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(payload);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task<string> ReceiveOneMessageAsync(CancellationToken cancellationToken)
    {
        if (_socket is null)
        {
            throw new InvalidOperationException("Socket not connected.");
        }

        byte[] buffer = new byte[16 * 1024];
        using MemoryStream messageStream = new();
        WebSocketReceiveResult result;

        do
        {
            result = await _socket.ReceiveAsync(buffer, cancellationToken);
            messageStream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(messageStream.ToArray());
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_socket is { State: WebSocketState.Open } && !cancellationToken.IsCancellationRequested)
            {
                string json = await ReceiveOneMessageAsync(cancellationToken);
                HandleMessage(json);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disconnect.
        }
        catch
        {
            // The socket dropped unexpectedly (OBS closed, network blip, etc).
        }
        finally
        {
            IsConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleMessage(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        int op = root.GetProperty("op").GetInt32();
        JsonElement data = root.GetProperty("d");

        switch (op)
        {
            case OpEvent:
            {
                string eventType = data.GetProperty("eventType").GetString() ?? "";
                JsonElement eventData = data.TryGetProperty("eventData", out JsonElement ed)
                    ? ed.Clone()
                    : default;

                EventReceived?.Invoke(this, (eventType, eventData));
                break;
            }

            case OpRequestResponse:
            {
                string requestId = data.GetProperty("requestId").GetString() ?? "";

                TaskCompletionSource<JsonElement>? completionSource;

                lock (_pendingRequestsLock)
                {
                    _pendingRequests.Remove(requestId, out completionSource);
                }

                if (completionSource is null)
                {
                    break;
                }

                JsonElement status = data.GetProperty("requestStatus");
                bool succeeded = status.GetProperty("result").GetBoolean();

                if (!succeeded)
                {
                    string comment = status.TryGetProperty("comment", out JsonElement c) ? c.GetString() ?? "" : "";
                    completionSource.TrySetException(new InvalidOperationException($"OBS request failed: {comment}"));
                    break;
                }

                JsonElement responseData = data.TryGetProperty("responseData", out JsonElement rd)
                    ? rd.Clone()
                    : default;

                completionSource.TrySetResult(responseData);
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
