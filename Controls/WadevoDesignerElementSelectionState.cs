namespace Wadevo.Controls;

public sealed class WadevoDesignerElementSelectionState
{
    private readonly List<WadevoDesignerElementState> _selectedElements = [];

    public event EventHandler<WadevoDesignerElementSelectionChangedEventArgs>? SelectionChanged;

    public IReadOnlyList<WadevoDesignerElementState> SelectedElements => _selectedElements;

    public WadevoDesignerElementState? PrimarySelection => _selectedElements.Count > 0
        ? _selectedElements[0]
        : null;

    public bool HasSelection => _selectedElements.Count > 0;

    public int Count => _selectedElements.Count;

    public bool IsSelected(WadevoDesignerElementState element)
    {
        return _selectedElements.Any(selected => selected.Id == element.Id);
    }

    public bool IsSelected(string id)
    {
        return _selectedElements.Any(selected => selected.Id == id);
    }

    public void Select(WadevoDesignerElementState element)
    {
        _selectedElements.Clear();
        _selectedElements.Add(element);

        RaiseSelectionChanged();
    }

    public void Add(WadevoDesignerElementState element)
    {
        if (IsSelected(element))
        {
            return;
        }

        _selectedElements.Add(element);

        RaiseSelectionChanged();
    }

    public void Remove(WadevoDesignerElementState element)
    {
        WadevoDesignerElementState? existing = _selectedElements.FirstOrDefault(selected => selected.Id == element.Id);

        if (existing is null)
        {
            return;
        }

        _selectedElements.Remove(existing);

        RaiseSelectionChanged();
    }

    public void Toggle(WadevoDesignerElementState element)
    {
        if (IsSelected(element))
        {
            Remove(element);
            return;
        }

        Add(element);
    }

    public void Clear()
    {
        if (_selectedElements.Count == 0)
        {
            return;
        }

        _selectedElements.Clear();

        RaiseSelectionChanged();
    }

    private void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(
            this,
            new WadevoDesignerElementSelectionChangedEventArgs(_selectedElements.ToArray()));
    }
}