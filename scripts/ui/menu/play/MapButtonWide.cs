using System;
using Godot;

public partial class MapButtonWide : MapButton
{
    /// <summary>
	/// Horizontal anchor offset when selected
	/// </summary>
    private float stickoutOffset = 0;

    private RichTextLabel extra;
    private ShaderMaterial coverMaterial;

    public override void _Ready()
    {
        base._Ready();

        extra = Holder.GetNode<RichTextLabel>("Extra");
        coverMaterial = Cover.Material as ShaderMaterial;

        CustomMinimumSize = new(CustomMinimumSize.X, MinimumSize);

        UpdateSkin();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        stickoutOffset = (float)Mathf.Lerp(stickoutOffset, Selected ? 0.05 : 0, Math.Min(1, 16 * delta));
        OutlineShader.SetShaderParameter("fill", OutlineFill);

        float mapListHalf = MapList.Instance.Size.Y / 2;
        float centerOffset = Math.Abs((GlobalPosition.Y - MapList.Instance.GlobalPosition.Y + Size.Y / 2) - mapListHalf) / (mapListHalf + MinimumSize / 2);
        centerOffset = (float)Math.Cos(Math.PI * centerOffset / 2);

        Holder.AnchorLeft = (float)(0.1 - centerOffset / 20 - stickoutOffset);
        CustomMinimumSize = new(CustomMinimumSize.X, (float)Mathf.Lerp(CustomMinimumSize.Y, MinimumSize + SizeOffset, Math.Min(1, 16 * delta)));
    }

    public override void UpdateInfo(Map map, bool selected = false)
    {
        base.UpdateInfo(map, selected);

        extra.Text = string.Format("[outline_size=2][outline_color=000000][color=808080]{0} — [color={1}]{2} [color=808080]by [color=b0b0b0]{3}",
            Util.String.FormatTime(map.Length / 1000),
            Constants.DIFFICULTY_COLORS[map.Difficulty].ToHtml(),
            map.DifficultyName,
            map.PrettyMappers
        );

        stickoutOffset = selected ? 0.05f : 0;
    }

    public override void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        base.UpdateSkin(skin);

        coverMaterial.Shader = skin.MapButtonCoverShader;
    }
}
