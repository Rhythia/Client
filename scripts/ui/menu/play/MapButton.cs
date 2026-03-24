using System;
using System.IO;
using Godot;

public partial class MapButton : Control, ISkinnable
{
    /// <summary>
    /// Parsed map reference
    /// </summary>
    public Map Map;

    /// <summary>
    /// Index within the full map list
    /// </summary>
    public int ListIndex = 0;

    /// <summary>
	/// Minimum Y size (configure in MapList properties)
	/// </summary>
    public float MinimumSize = 90;

    /// <summary>
	/// Additional Y size when hovered (configure in MapList properties)
	/// </summary>
    public float HoveredSizeOffset = 10;

    /// <summary>
    /// Additional Y size when selected (configure in MapList properties)
    /// </summary>
    public float SelectedSizeOffset = 20;

    /// <summary>
	/// Total Y size added on top of minimum size, equivalent to HoverSizeOffset + SelectedSizeOffset
	/// </summary>
    public float SizeOffset = 0;

    public bool Hovered = false;
    public bool Selected = false;
    public float OutlineFill = 0;
    public Vector2 LightPosition = Vector2.Zero;

    [Signal]
    public delegate void MouseHoveredEventHandler(bool hovered);

    [Signal]
    public delegate void PressedEventHandler();

    public HBoxContainer Container;
    public Panel Holder;
    public Label Title;
    public TextureRect Cover;
    public TextureRect Favorited;
    public Button Button;
    public ShaderMaterial OutlineShader;

    private float targetOutlineFill = 0;

    public override void _Ready()
    {
        Holder = GetNode<Panel>("Holder");
        Button = Holder.GetNode<Button>("Button");
        Title = Holder.GetNode<Label>("Title");
        Cover = Holder.GetNode<TextureRect>("Cover");
        Favorited = Holder.GetNode<TextureRect>("Favorited");
        Favorited.Texture = (Texture2D)Favorited.Texture.Duplicate();

        Panel outline = Holder.GetNode<Panel>("Outline");

        OutlineShader = (ShaderMaterial)outline.Material.Duplicate();
        outline.Material = OutlineShader;

        Button.MouseEntered += () => { Hover(true); };
        Button.MouseExited += () => { Hover(false); };
        Button.Pressed += () => { EmitSignal(SignalName.Pressed); };

        SkinManager.Instance.Loaded += UpdateSkin;
    }

    public override void _Process(double delta)
    {
        OutlineFill = (float)Mathf.Lerp(OutlineFill, targetOutlineFill, Math.Min(1, 10 * delta));

        float now = (float)Time.GetTicksMsec();

        Favorited.RotationDegrees = ListIndex * -10 + now / 20;
        Favorited.Modulate = Color.Color8(255, 255, 255, (byte)(225 + 30 * Math.Sin(Math.PI * now / 2000 + ListIndex)));

        OutlineShader.SetShaderParameter("cursor_position", GetViewport().GetMousePosition());
        OutlineShader.SetShaderParameter("light_position", LightPosition);
    }

    public virtual void Hover(bool hover)
    {
        Hovered = hover;
        SizeOffset = computeSizeOffset();

        EmitSignal(SignalName.MouseHovered, hover);

        CreateTween().SetTrans(Tween.TransitionType.Quad).TweenProperty(Holder, "self_modulate", Hovered ? Color.Color8(26, 6, 13, 224) : Color.Color8(0, 0, 0, 224), 0.15);
    }

    public virtual void Select(bool select = true)
    {
        Selected = select;
        SizeOffset = computeSizeOffset();

        CreateTween().SetTrans(Tween.TransitionType.Quad).TweenProperty(Cover, "modulate", Color.Color8(255, 255, 255, (byte)(Selected ? 255 : 128)), 0.1);
    }

    public void Deselect()
    {
        Select(false);
    }

    public virtual void UpdateInfo(Map map, bool selected = false)
    {
        Map = map;
        Name = map.Name;

        Title.Text = map.PrettyTitle;
        Favorited.Visible = map.Favorite;
        Cover.Texture = map.Cover;
        Favorited.SelfModulate = Constants.DIFFICULTY_COLORS[Math.Clamp(map.Difficulty, 0, Constants.DIFFICULTY_COLORS.Length - 1)];

        if (selected)
        {
            Select();
            UpdateOutline(1f, 0f);
        }
    }

    public void UpdateOutline(float targetFill, float fill = -1)
    {
        targetOutlineFill = targetFill;

        if (fill != -1)
        {
            OutlineFill = fill;
        }
    }

    public virtual void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        Favorited.Texture = skin.FavoriteImage;
    }

    private float computeSizeOffset()
    {
        return (Hovered ? HoveredSizeOffset : 0) + (Selected ? SelectedSizeOffset : 0);
    }
}
