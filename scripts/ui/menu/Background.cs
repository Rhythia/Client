using System;
using Godot;

public partial class Background : Panel, ISkinnable
{
    private TextureRect tile;
    private ShaderMaterial tileMaterial;

    public override void _Ready()
    {
        tile = GetNode<TextureRect>("Tile");
        tileMaterial = tile.Material as ShaderMaterial;

        SkinManager.Instance.Loaded += UpdateSkin;

        UpdateSkin();
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        tile.Texture = skin.BackgroundTileImage;
        tileMaterial.Shader = skin.BackgroundTileShader;
    }
}
