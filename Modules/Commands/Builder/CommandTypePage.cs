namespace Wadevo.Modules.Commands.Builder;

using Wadevo.Controls;

public class CommandTypePage : CommandBuilderPage
{
    private readonly List<WadevoSelectionCard> _cards = new();
    private string _selectedType = "Chat Message";

    public override string PageTitle => "✨ Wadevo Builder";
    public override string PageSubtitle => "What would you like this command to do?";

    public CommandTypePage()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        AddTypeCard("💬", "Chat Message", "Send text into chat.", 45, 25);
        AddTypeCard("🖼", "GIF / Image", "Display an image or GIF.", 295, 25);
        AddTypeCard("🎬", "Video Clip", "Play a short video.", 545, 25);

        AddTypeCard("🔊", "Sound Effect", "Play an audio file.", 45, 145);
        AddTypeCard("🎉", "Multi Action", "Run several actions.", 295, 145);
        AddTypeCard("🎥", "Change OBS Scene", "Switch OBS to a specific scene.", 545, 145);
    }

    public override void LoadFromState(BuilderState state)
    {
        _selectedType = state.CommandType;
        UpdateCards();
    }

    public override void SaveToState(BuilderState state)
    {
        state.CommandType = _selectedType;
    }

    private void AddTypeCard(string icon, string title, string description, int x, int y)
    {
        WadevoSelectionCard card = new()
        {
            IconText = icon,
            TitleText = title,
            DescriptionText = description,
            Tag = title,
            Location = new Point(x, y),
            Size = new Size(210, 96)
        };

        card.CardClicked += (_, _) =>
        {
            _selectedType = title;
            UpdateCards();
        };

        _cards.Add(card);
        Controls.Add(card);
    }

    private void UpdateCards()
    {
        foreach (WadevoSelectionCard card in _cards)
        {
            card.IsSelected = card.Tag?.ToString() == _selectedType;
        }
    }
}