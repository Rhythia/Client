using Godot;

public partial class ReleaseNotesButton : LinkPopupButton
{
    public override void _Ready()
    {
        base._Ready();

        string version = (string)ProjectSettings.GetSetting("application/config/version");

        UpdateLink(string.Format(Link, version));
    }
}
