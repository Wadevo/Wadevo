namespace Wadevo.Controls;

public sealed class WadevoDesignerDocumentController
{
    private readonly WadevoDesignerDocumentRenderer _renderer = new();
    private readonly WadevoDesignerDocumentService _documentService = new();
    private readonly WadevoDesignerElementSelectionState _selectionState = new();

    public event EventHandler<WadevoDesignerDocumentChangedEventArgs>? DocumentChanged;
    public event EventHandler<WadevoDesignerElementSelectionChangedEventArgs>? SelectionChanged;

    public WadevoDesignerDocument Document => _documentService.CurrentDocument;

    public IReadOnlyDictionary<string, WadevoDesignerElementControl> Controls =>
        _renderer.ControlsById;

    public IReadOnlyList<WadevoDesignerElementState> SelectedElements =>
        _selectionState.SelectedElements;

    public WadevoDesignerElementState? PrimarySelection =>
        _selectionState.PrimarySelection;

    public bool HasSelection => _selectionState.HasSelection;

    public int SelectionCount => _selectionState.Count;

    public WadevoDesignerDocumentController()
    {
        _selectionState.SelectionChanged += SelectionState_SelectionChanged;
    }

    public void Load(Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _documentService.Load();
        _renderer.Render(Document, canvas);
        RefreshSelectionVisuals();

        RaiseDocumentChanged();
    }

    public void Save()
    {
        _documentService.Save(Document);
    }

    public void Render(Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _renderer.Render(Document, canvas);
        RefreshSelectionVisuals();
    }

    public WadevoDesignerElementControl? FindControl(string id)
    {
        return _renderer.FindControl(id);
    }

    public WadevoDesignerElementState? FindElement(string id)
    {
        return Document.Find(id);
    }

    public WadevoDesignerElementState Add(WadevoDesignerElementState element, Control canvas)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(canvas);

        Document.Add(element);

        _renderer.Render(Document, canvas);
        Save();

        RaiseDocumentChanged(element);

        return element;
    }

    public bool Remove(string id, Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        bool removed = Document.Remove(id);

        if (!removed)
        {
            return false;
        }

        _selectionState.Clear();

        _renderer.Render(Document, canvas);
        Save();

        RaiseDocumentChanged();

        return true;
    }

    public void Refresh(Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _renderer.Render(Document, canvas);
        RefreshSelectionVisuals();
    }

    public void Clear(Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _selectionState.Clear();
        Document.Clear();
        _renderer.Clear(canvas);
        Save();

        RaiseDocumentChanged();
    }

    public void BringToFront(string id, Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        Document.BringToFront(id);
        _renderer.Render(Document, canvas);
        RefreshSelectionVisuals();
        Save();

        RaiseDocumentChanged(FindElement(id));
    }

    public void SendToBack(string id, Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        Document.SendToBack(id);
        _renderer.Render(Document, canvas);
        RefreshSelectionVisuals();
        Save();

        RaiseDocumentChanged(FindElement(id));
    }

    public void Select(string id)
    {
        WadevoDesignerElementState? element = FindElement(id);

        if (element is null)
        {
            return;
        }

        _selectionState.Select(element);
    }

    public void ToggleSelection(string id)
    {
        WadevoDesignerElementState? element = FindElement(id);

        if (element is null)
        {
            return;
        }

        _selectionState.Toggle(element);
    }

    public void SelectElements(IEnumerable<WadevoDesignerElementState> elements)
    {
        WadevoDesignerElementState[] selected = elements.ToArray();

        _selectionState.Clear();

        foreach (WadevoDesignerElementState element in selected)
        {
            _selectionState.Add(element);
        }
    }

    public void ClearSelection()
    {
        _selectionState.Clear();
    }

    public bool IsSelected(string id)
    {
        return _selectionState.IsSelected(id);
    }

    private void SelectionState_SelectionChanged(object? sender, WadevoDesignerElementSelectionChangedEventArgs e)
    {
        RefreshSelectionVisuals();

        SelectionChanged?.Invoke(this, e);
    }

    private void RefreshSelectionVisuals()
    {
        foreach (KeyValuePair<string, WadevoDesignerElementControl> pair in _renderer.ControlsById)
        {
            pair.Value.IsSelected = _selectionState.IsSelected(pair.Key);
            pair.Value.Invalidate();
        }
    }

    private void RaiseDocumentChanged(WadevoDesignerElementState? changedElement = null)
    {
        DocumentChanged?.Invoke(
            this,
            new WadevoDesignerDocumentChangedEventArgs(
                Document,
                changedElement));
    }
}