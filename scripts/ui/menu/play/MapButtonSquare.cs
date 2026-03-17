using System;
using Godot;

public partial class MapButtonSquare : MapButton
{
    private Color idleColor = Color.Color8(13, 13, 13);
    private Color hoverColor = Color.Color8(50, 50, 50);
    private Color selectColor = Color.Color8(201, 0, 69);

    private Label difficulty;
    private TextureRect coverBackground;

    public override void _Ready()
    {
        base._Ready();

        difficulty = Holder.GetNode<Label>("Difficulty");
        difficulty.LabelSettings = difficulty.LabelSettings.Duplicate() as LabelSettings;
        coverBackground = Holder.GetNode<TextureRect>("CoverBackground");

        UpdateSkin();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        float half = SizeOffset / 2;
        float alpha = (float)Math.Min(1, 16 * delta);

        Holder.OffsetLeft = Mathf.Lerp(Holder.OffsetLeft, -half, alpha);
        Holder.OffsetRight = Mathf.Lerp(Holder.OffsetRight, half, alpha);
        Holder.OffsetTop = Mathf.Lerp(Holder.OffsetTop, -half, alpha);
        Holder.OffsetBottom = Mathf.Lerp(Holder.OffsetBottom, half, alpha);
        CustomMinimumSize = Vector2.One * MinimumSize;
    }

    public override void Hover(bool hover)
    {
        base.Hover(hover);

        updateFocus();
    }

    public override void Select(bool select = true)
    {
        base.Select(select);

        updateFocus();
    }

    public override void UpdateInfo(Map map, bool selected = false)
    {
        base.UpdateInfo(map, selected);

        var color = Constants.DIFFICULTY_COLORS[map.Difficulty];

        difficulty.Text = map.DifficultyName;
        difficulty.LabelSettings.FontColor = color;
        coverBackground.SelfModulate = color;
    }

    public override void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        base.UpdateSkin(skin);

        coverBackground.Texture = skin.MapListGridCoverBackgroundImage;
    }

    private void updateFocus()
    {
        Cover.Modulate = Color.Color8(255, 255, 255, (byte)(Hovered || Selected ? 255 : 128));
        OutlineShader.SetShaderParameter("outline_color", Selected ? selectColor : Hovered ? hoverColor : idleColor);
    }
}
