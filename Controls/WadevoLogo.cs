namespace Wadevo.Controls;

public class WadevoLogo : PictureBox
{
    public string LogoFile { get; set; } = "small wadevo logo.png";

    public WadevoLogo()
    {
        SizeMode = PictureBoxSizeMode.Zoom;
        BackColor = Color.Transparent;
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        LoadLogo();
    }

    public void LoadLogo()
    {
        string path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets",
            "Logos",
            LogoFile);

        if (!File.Exists(path))
        {
            return;
        }

        Image?.Dispose();
        Image = Image.FromFile(path);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Image?.Dispose();
        }

        base.Dispose(disposing);
    }
}