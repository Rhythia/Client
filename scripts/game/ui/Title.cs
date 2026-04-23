using Godot;
using System;

public partial class Title : UIComponent
{
	private Label3D label;

	public override void Init()
	{
		label = GetNode<Label3D>("Label");
		label.Text = Runner.Attempt.Map.PrettyTitle;
	}
}
