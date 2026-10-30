using System;
using Godot;

public partial class PanelRightPreview : UIComponent
{
    public static PanelRightPreview Instance;
    private SubViewport viewport;
    private Label accuracy,
        hits,
        misses,
        simpleMisses,
        sum;

    public override void _Ready()
    {
        Instance = this;
        var settings = SettingsManager.Instance.Settings;
        bool isVisible = !settings.SimpleHUD && !settings.SuperSimpleHUD;

        viewport = GetNode<SubViewport>("PanelRightViewport");
        accuracy = viewport.GetNode<Label>("Accuracy");
        hits = viewport.GetNode<Label>("Hits");
        misses = viewport.GetNode<Label>("Misses");
        simpleMisses = viewport.GetNode<Label>("SimpleMisses");
        sum = viewport.GetNode<Label>("Sum");

        // this once is for init purposes, updateskin ofc only reflects the skin once its updated

        Godot.Collections.Array<Node> widgets = viewport.GetChildren();
        foreach (Node widget in widgets)
        {
            (widget as CanvasItem).Visible = isVisible;
        }
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelRightBackgroundImage;
        viewport.GetNode<TextureRect>("HitsIcon").Texture = SkinManager.Instance.Skin.HitsImage;
        viewport.GetNode<TextureRect>("MissesIcon").Texture = SkinManager.Instance.Skin.MissesImage;

        simpleMisses.Visible = !isVisible;
        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        var settings = SettingsManager.Instance.Settings;
        bool isVisible = !settings.SimpleHUD && !settings.SuperSimpleHUD;

        Godot.Collections.Array<Node> widgets = viewport.GetChildren();
        foreach (Node widget in widgets)
        {
            (widget as CanvasItem).Visible = isVisible;
        }

        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelRightBackgroundImage;
        viewport.GetNode<TextureRect>("HitsIcon").Texture = SkinManager.Instance.Skin.HitsImage;
        viewport.GetNode<TextureRect>("MissesIcon").Texture = SkinManager.Instance.Skin.MissesImage;

        simpleMisses.Visible = !isVisible;
    }
}
