namespace Wadevo.Controls;

public sealed class WadevoDesignerDocumentService
{
    private readonly WadevoDesignerDocumentStore _store = new();

    public WadevoDesignerDocument CurrentDocument { get; private set; } = new();

    public event EventHandler<WadevoDesignerDocumentChangedEventArgs>? DocumentLoaded;
    public event EventHandler<WadevoDesignerDocumentChangedEventArgs>? DocumentSaved;

    public WadevoDesignerDocument NewDocument()
    {
        CurrentDocument = new WadevoDesignerDocument();

        RaiseLoaded();

        return CurrentDocument;
    }

    public WadevoDesignerDocument Load()
    {
        CurrentDocument = _store.Load();

        RaiseLoaded();

        return CurrentDocument;
    }

    public void Save()
    {
        _store.Save(CurrentDocument);

        RaiseSaved();
    }

    public void Save(WadevoDesignerDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        CurrentDocument = document;

        _store.Save(document);

        RaiseSaved();
    }

    private void RaiseLoaded()
    {
        DocumentLoaded?.Invoke(
            this,
            new WadevoDesignerDocumentChangedEventArgs(CurrentDocument));
    }

    private void RaiseSaved()
    {
        DocumentSaved?.Invoke(
            this,
            new WadevoDesignerDocumentChangedEventArgs(CurrentDocument));
    }
}