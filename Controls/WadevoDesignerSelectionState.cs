namespace Wadevo.Controls;

public sealed class WadevoDesignerSelectionState
{
    private readonly List<Control> _selectedControls = [];

    public event EventHandler? SelectionChanged;

    public IReadOnlyList<Control> SelectedControls => _selectedControls;

    public Control? PrimarySelection => _selectedControls.Count > 0
        ? _selectedControls[0]
        : null;

    public bool HasSelection => _selectedControls.Count > 0;

    public int Count => _selectedControls.Count;

    public bool IsSelected(Control control)
    {
        return _selectedControls.Contains(control);
    }

    public void Select(Control control)
    {
        _selectedControls.Clear();
        _selectedControls.Add(control);

        RaiseSelectionChanged();
    }

    public void Add(Control control)
    {
        if (_selectedControls.Contains(control))
        {
            return;
        }

        _selectedControls.Add(control);

        RaiseSelectionChanged();
    }

    public void Remove(Control control)
    {
        if (!_selectedControls.Remove(control))
        {
            return;
        }

        RaiseSelectionChanged();
    }

    public void Toggle(Control control)
    {
        if (_selectedControls.Contains(control))
        {
            _selectedControls.Remove(control);
        }
        else
        {
            _selectedControls.Add(control);
        }

        RaiseSelectionChanged();
    }

    public void Clear()
    {
        if (_selectedControls.Count == 0)
        {
            return;
        }

        _selectedControls.Clear();

        RaiseSelectionChanged();
    }

    private void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}