using System.Globalization;
using Godot;

public partial class VersionLabel : RichTextLabel
{
    public override void _Ready()
    {
        string version = (string)ProjectSettings.GetSetting("application/config/version");

        Text = string.Format(CultureInfo.CurrentCulture, Text, version);
    }
}
