using System;
using Godot;

public partial class TitlePreview : UIComponent
{
    public static TitlePreview Instance;

    private Label3D label;

    public override void _Ready()
    {
        Instance = this;

        var settings = SettingsManager.Instance.Settings;

        label = GetNode<Label3D>("Label");
        label.Text = "Long Artist - Title Long Enough For You To Get The Hint That It's A Placeholder";
        label.Visible = !settings.SuperSimpleHUD;

        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        var settings = SettingsManager.Instance.Settings;
        label.Visible = !settings.SuperSimpleHUD;
    }
}
