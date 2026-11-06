using System;
using Godot;
using util;

public partial class SoundSpaceButton : Button
{
    private Label label;

    public override void _Ready()
    {
        label = GetNode<Label>("Text/Pt2");

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public void OnMouseEntered()
    {
        label.SelfModulate = Color.FromHtml("ffffffff");
    }

    public void OnMouseExited()
    {
        label.SelfModulate = Color.FromHtml("ffffff40");
    }
}
