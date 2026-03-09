using System;
using Godot;
using Godot.Collections;

public partial class LinkPopupButton : Button
{
    public static string InfoTemplate = "[center][color=dddddd]This will open the following link:\n[color=aaaaff][i][u]{0}[/u][/i]\n\n[color=dddddd]Are you sure?";

	[Export]
	public string Link;

	private OptionPopup popup;

	public override void _EnterTree()
	{
		base._EnterTree();

		popup = new("Open Link", "");

		UpdateLink(Link);

		popup.AddOption("Open", Callable.From(() => { OS.ShellOpen(Link); }), Link);
		popup.AddOption("Cancel", Callable.From(popup.Hide));
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		popup?.QueueFree();
	}

	public override void _Ready()
	{
		base._Ready();

		Pressed += Press;
	}

	public void Press()
	{
		popup?.Show();
	}

	public void UpdateLink(string link)
	{
		Link = link;

		if (popup != null)
		{
			if (popup.Options.TryGetValue("Open", out Button button))
			{
				button.TooltipText = link;
			}

			popup.UpdateInfo(string.Format(InfoTemplate, link));
		}
	}
}
