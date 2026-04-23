using Godot;
using System;
using System.Collections.Generic;

public partial class Speed : UIComponent
{
	private Label3D label;

	public override void Init()
	{
		label = GetNode<Label3D>("Label");

		label.Modulate = Color.Color8(255, 255, 255, (byte)(Runner.Attempt.Speed == 1 ? 0 : 100));
		label.Text = $"{Runner.Attempt.Speed.ToString():F2}x";
	}
}
