namespace Wadevo.Controls;

public sealed class WadevoDesignerDocumentRenderer
{
    private readonly Dictionary<string, WadevoDesignerElementControl> _controlsById = [];

    public IReadOnlyDictionary<string, WadevoDesignerElementControl> ControlsById => _controlsById;

    public void Render(WadevoDesignerDocument document, Control canvas)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(canvas);

        canvas.SuspendLayout();

        try
        {
            RemoveDeletedControls(document, canvas);
            AddMissingControls(document, canvas);
            RefreshControls(document);
            ApplyLayerOrder(document);
        }
        finally
        {
            canvas.ResumeLayout();
        }
    }

    public WadevoDesignerElementControl? FindControl(string id)
    {
        return _controlsById.TryGetValue(id, out WadevoDesignerElementControl? control)
            ? control
            : null;
    }

    public void Clear(Control canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        foreach (WadevoDesignerElementControl control in _controlsById.Values.ToList())
        {
            canvas.Controls.Remove(control);
            control.Dispose();
        }

        _controlsById.Clear();
    }

    private void RemoveDeletedControls(WadevoDesignerDocument document, Control canvas)
    {
        HashSet<string> documentIds = document.Elements
            .Where(element => !string.IsNullOrWhiteSpace(element.Id))
            .Select(element => element.Id)
            .ToHashSet();

        foreach (string id in _controlsById.Keys.ToList())
        {
            if (documentIds.Contains(id))
            {
                continue;
            }

            WadevoDesignerElementControl control = _controlsById[id];

            canvas.Controls.Remove(control);
            control.Dispose();

            _controlsById.Remove(id);
        }
    }

    private void AddMissingControls(WadevoDesignerDocument document, Control canvas)
    {
        foreach (WadevoDesignerElementState element in document.Elements)
        {
            if (string.IsNullOrWhiteSpace(element.Id))
            {
                continue;
            }

            if (_controlsById.ContainsKey(element.Id))
            {
                continue;
            }

            WadevoDesignerElementControl control = WadevoDesignerElementControlFactory.Create(element);

            _controlsById[element.Id] = control;
            canvas.Controls.Add(control);
        }
    }

    private void RefreshControls(WadevoDesignerDocument document)
    {
        foreach (WadevoDesignerElementState element in document.Elements)
        {
            if (string.IsNullOrWhiteSpace(element.Id))
            {
                continue;
            }

            if (!_controlsById.TryGetValue(element.Id, out WadevoDesignerElementControl? control))
            {
                continue;
            }

            control.RefreshFromState();
        }
    }

    private void ApplyLayerOrder(WadevoDesignerDocument document)
    {
        foreach (WadevoDesignerElementState element in document.Elements.Reverse<WadevoDesignerElementState>())
        {
            if (string.IsNullOrWhiteSpace(element.Id))
            {
                continue;
            }

            if (_controlsById.TryGetValue(element.Id, out WadevoDesignerElementControl? control))
            {
                control.BringToFront();
            }
        }
    }
}