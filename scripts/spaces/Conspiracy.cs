using System;
using Godot;

namespace Spaces;

public partial class Conspiracy : BaseSpace
{
    private SettingsProfile settings;
    private Node3D icons;
    private Vector3 iconsStartPos;
    private Node3D scrollingText;
    private Vector3 scrollingTextStartPos;
    private Node3D scrollingQuestionMarks;
    private StandardMaterial3D scrollingQuestionMarkTexture;
    private const float scrolling_text_loop_end = 46f;

    public override void _Ready()
    {
        settings = SettingsManager.Instance.Settings;

        icons = GetNode<Node3D>("Icons");
        iconsStartPos = icons.Position;
        scrollingQuestionMarks = GetNode<Node3D>("ScrollingQuestionMarks");
        scrollingQuestionMarkTexture = (scrollingQuestionMarks.GetNode<MeshInstance3D>("Left").Mesh as PlaneMesh).Material as StandardMaterial3D;
        scrollingText = GetNode<Node3D>("ScrollingText");
        scrollingTextStartPos = scrollingText.Position;

        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (settings.SpaceEffects)
        {
            scrollingQuestionMarkTexture.Uv1Offset += Vector3.Up * (float)delta / 10;

            icons.Position = iconsStartPos + Vector3.Up * (float)Mathf.Sin(Time.GetTicksMsec() / 1000f);

            if (scrollingText.Position.Z < scrolling_text_loop_end)
            {
                scrollingText.Position += Vector3.Back * (float)(delta * 2);
            }
            else
            {
                scrollingText.Position = scrollingTextStartPos;
            }
        }
        else
        {
            scrollingText.Visible = settings.SpaceEffects;
        }
    }
}
