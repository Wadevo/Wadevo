namespace Wadevo.Media.Services;

using Wadevo.Media.Models;

public sealed class MediaLibraryService
{
    private readonly List<MediaAssetModel> _assets = new();

    public IReadOnlyList<MediaAssetModel> Assets => _assets;

    public MediaAssetModel Add(MediaAssetModel asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        _assets.Add(asset);

        return asset;
    }

    public void Remove(MediaAssetModel asset)
    {
        _assets.Remove(asset);
    }

    public void Clear()
    {
        _assets.Clear();
    }

    public IEnumerable<MediaAssetModel> FindByCategory(string category)
    {
        return _assets.Where(asset =>
            asset.Category.Equals(
                category,
                StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<MediaAssetModel> Search(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _assets;

        return _assets.Where(asset =>
            asset.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            asset.Tags.Contains(text, StringComparison.OrdinalIgnoreCase));
    }
}