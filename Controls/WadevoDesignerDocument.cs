namespace Wadevo.Controls;

using System.Collections.ObjectModel;

public sealed class WadevoDesignerDocument
{
    private readonly ObservableCollection<WadevoDesignerElementState> _elements = [];

    public ObservableCollection<WadevoDesignerElementState> Elements => _elements;

    public WadevoDesignerElementState? Find(string id)
    {
        return _elements.FirstOrDefault(element => element.Id == id);
    }

    public void Add(WadevoDesignerElementState element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (_elements.Any(item => item.Id == element.Id))
        {
            return;
        }

        _elements.Add(element);
    }

    public bool Remove(string id)
    {
        WadevoDesignerElementState? element = Find(id);

        if (element is null)
        {
            return false;
        }

        _elements.Remove(element);
        return true;
    }

    public void Clear()
    {
        _elements.Clear();
    }

    public void BringToFront(string id)
    {
        WadevoDesignerElementState? element = Find(id);

        if (element is null)
        {
            return;
        }

        _elements.Remove(element);
        _elements.Add(element);
    }

    public void SendToBack(string id)
    {
        WadevoDesignerElementState? element = Find(id);

        if (element is null)
        {
            return;
        }

        _elements.Remove(element);
        _elements.Insert(0, element);
    }
}