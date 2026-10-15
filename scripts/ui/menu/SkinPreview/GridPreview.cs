using System;
using System.Globalization;
using Godot;

public partial class GridPreview : MeshInstance3D, ISkinnable
{
    public static GridPreview Instance;

    [Export]
    public MeshInstance3D Cursor { get; set; }
    public MultiMeshInstance3D CursorTrail { get; set; }
    public MeshInstance3D GridGuides { get; set; }
    public MeshInstance3D VisibilityAssist { get; set; }

    public override void _Ready()
    {
        Instance = this;

        var settings = SettingsManager.Instance.Settings;

        VisibilityAssist ??= GetNode<MeshInstance3D>("VisibilityAssist");
        GridGuides ??= GetNode<MeshInstance3D>("GridGuides");
        Cursor ??= GetNode<MeshInstance3D>("Cursor");

        (VisibilityAssist.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.VisibilityAssistImage;
        (GridGuides.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridGuidesImage;
        (Cursor.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;
        (GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridImage;

        VisibilityAssist.Visible = settings.VisibilityAssist;
        GridGuides.Visible = settings.GridGuides;

        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        var settings = SettingsManager.Instance.Settings;

        VisibilityAssist.Visible = settings.VisibilityAssist;
        GridGuides.Visible = settings.GridGuides;

        (VisibilityAssist.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.VisibilityAssistImage;
        (GridGuides.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridGuidesImage;
        (Cursor.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;
        (GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridImage;
    }
}
