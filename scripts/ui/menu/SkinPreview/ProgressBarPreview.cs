using System;
using Godot;

public partial class ProgressBarPreview : UIComponent
{
    public static ProgressBarPreview Instance;

    private SubViewport viewport;
    private TextureRect progressBarTexture;
    private TextureRect progressBarBGTexture;

    public override void _Ready()
    {
        Instance = this;

        viewport = GetNode<SubViewport>("ProgressBarViewport");
        progressBarTexture = viewport.GetNode<TextureRect>("Main");
        progressBarBGTexture = viewport.GetNode<TextureRect>("Background");

        // this once is for init purposes, updateskin ofc only reflects the skin once its updated
        viewport.GetNode<TextureRect>("Main").Texture = SkinManager.Instance.Skin.ProgressImage;
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.ProgressBackgroundImage;

        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        viewport.GetNode<TextureRect>("Main").Texture = SkinManager.Instance.Skin.ProgressImage;
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.ProgressBackgroundImage;
    }
}
