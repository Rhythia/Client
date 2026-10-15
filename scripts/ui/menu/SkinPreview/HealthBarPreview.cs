using System;
using Godot;

public partial class HealthBarPreview : UIComponent
{
    public static HealthBarPreview Instance;

    private SubViewport viewport;
    private TextureRect healthBarTexture;
    private TextureRect healthBarBGTexture;

    public override void _Ready()
    {
        Instance = this;

        var settings = SettingsManager.Instance.Settings;

        viewport = GetNode<SubViewport>("HealthViewport");
        healthBarTexture = viewport.GetNode<TextureRect>("Main");
        healthBarBGTexture = viewport.GetNode<TextureRect>("Background");
        healthBarTexture.Modulate = new(0xffffffff);

        // this once is for init purposes, updateskin ofc only reflects the skin once its updated
        viewport.GetNode<TextureRect>("Main").Texture = SkinManager.Instance.Skin.HealthImage;
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.HealthBackgroundImage;

        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        var settings = SettingsManager.Instance.Settings;

        healthBarTexture.Visible = !settings.SuperSimpleHUD;
        healthBarBGTexture.Visible = !settings.SuperSimpleHUD;

        viewport.GetNode<TextureRect>("Main").Texture = SkinManager.Instance.Skin.HealthImage;
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.HealthBackgroundImage;
    }
}
