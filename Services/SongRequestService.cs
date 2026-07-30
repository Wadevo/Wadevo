namespace Wadevo.Services;

using Wadevo.Models;

public sealed class SongRequestService
{
    private readonly SongRequestSettingsStore _settingsStore = new();
    private readonly SongRequestQueueStore _queueStore = new();
    private readonly List<SongRequestModel> _queue;

    public event EventHandler? QueueChanged;

    public SongRequestService()
    {
        _queue = _queueStore.Load();
    }

    public SongRequestSettings GetSettings()
    {
        return _settingsStore.Load();
    }

    public void SaveSettings(SongRequestSettings settings)
    {
        _settingsStore.Save(settings);
    }

    public IReadOnlyList<SongRequestModel> GetQueue()
    {
        return _queue.ToList();
    }

    // Returns null if the request couldn't be added (queue full or disabled), otherwise the
    // added request - the caller uses this to know whether/what to confirm back to chat.
    public SongRequestModel? TryAddRequest(string requesterUsername, string songText)
    {
        SongRequestSettings settings = GetSettings();

        if (!settings.IsEnabled || string.IsNullOrWhiteSpace(songText))
        {
            return null;
        }

        int pendingCount = _queue.Count(r => !r.IsPlayed);

        if (pendingCount >= settings.MaxQueueSize)
        {
            return null;
        }

        SongRequestModel request = new()
        {
            RequesterUsername = requesterUsername,
            SongText = songText.Trim()
        };

        _queue.Add(request);
        Persist();

        return request;
    }

    public void MarkPlayed(Guid id)
    {
        SongRequestModel? request = _queue.FirstOrDefault(r => r.Id == id);

        if (request is null)
        {
            return;
        }

        request.IsPlayed = true;
        Persist();
    }

    public void Remove(Guid id)
    {
        _queue.RemoveAll(r => r.Id == id);
        Persist();
    }

    public void ClearPlayed()
    {
        _queue.RemoveAll(r => r.IsPlayed);
        Persist();
    }

    public void ClearAll()
    {
        _queue.Clear();
        Persist();
    }

    private void Persist()
    {
        _queueStore.Save(_queue);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }
}
