using System;
using Godot;

public partial class PanelLeftPreview : UIComponent
{
    public static PanelLeftPreview Instance;

    private SubViewport viewport;

    private Label score,
        multiplier;
    private Label altCombo;

    public override void _Ready()
    {
        Instance = this;
        var settings = SettingsManager.Instance.Settings;
        bool isVisible = !settings.SimpleHUD && !settings.SuperSimpleHUD;

        viewport = GetNode<SubViewport>("PanelLeftViewport");
        multiplier = viewport.GetNode<Label>("Multiplier");
        altCombo = viewport.GetNode<Label>("ComboCount");

        altCombo.Visible = isVisible && settings.AltComboCounter;

        // this once is for init purposes, updateskin ofc only reflects the skin once its updated
        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelLeftBackgroundImage;

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

        altCombo.Visible = isVisible && settings.AltComboCounter;

        viewport.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelLeftBackgroundImage;
    }
}
