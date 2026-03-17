using System;
using Godot;

public partial class SoundSpaceButton : Button
{
    private RichTextLabel label;

    public override void _Ready()
    {
        label = GetNode<RichTextLabel>("RichTextLabel");

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    public void OnMouseEntered()
    {
        label.Text = "[center][color=ffffff40]Inspired by [color=ffffffff]Sound Space";
    }

    public void OnMouseExited()
    {
        label.Text = "[center][color=ffffff40]Inspired by Sound Space";
    }
}
