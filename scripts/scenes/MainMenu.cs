using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainMenu : BaseScene
{
	public Panel CurrentMenu;
	public Panel LastMenu;

    public Panel HomeMenu;
    public Panel PlayMenu;
    public Panel TopBar;
    public JukeboxPanel Jukebox;
    public MapList MapList;
    public MapInfo MapInfo;

    private Panel menuHolder;
    private Node topBarButtonsContainer;

    public override void _Ready()
	{
        base._Ready();

        menuHolder = GetNode<Panel>("Menus");

        HomeMenu = menuHolder.GetNode<Panel>("Home");
        PlayMenu = menuHolder.GetNode<Panel>("Play");
        LastMenu = HomeMenu;

        TopBar = GetNode<Panel>("TopBar");
        topBarButtonsContainer = TopBar.GetNode("MenuButtons");
        Jukebox = GetNode<JukeboxPanel>("JukeboxPanel");
        MapList = PlayMenu.GetNode<MapList>("MapList");
        MapInfo = PlayMenu.GetNode<MapInfo>("MapInfo");

        CurrentMenu = HomeMenu;

        Input.MouseMode = SettingsManager.Instance.Settings.UseCursorInMenus ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;

        List<Node> menuButtons = [.. HomeMenu.GetNode("Buttons").GetChildren()];
        menuButtons.AddRange(topBarButtonsContainer.GetChildren());

        foreach (Button button in menuButtons)
		{
			Panel menu = (Panel)menuHolder.FindChild(button.Name, false);

			if (menu != null)
			{
				button.Pressed += () => { Transition(menu); };
			}
		}
    }

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			switch (mouseButton.ButtonIndex)
			{
				case MouseButton.Xbutton1:
					Transition(HomeMenu);
					break;
				case MouseButton.Xbutton2:
					Transition(LastMenu);
					break;
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed)
		{
			switch (key.Keycode)
			{
				case Key.Escape:
                    Transition(HomeMenu);
                    break;
            }
		}
	}

	public override void Load()
	{
        base.Load();

        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Adaptive);

        MapInfo.InfoContainer?.Refresh();
		SceneManager.Space?.UpdateState(false);

        var map = MapManager.Selected.Value;

        if (map != null)
        {
            SceneManager.Space?.UpdateMap(map);
        }
    }

	public void Transition(Panel menu, bool instant = false)
	{
		if (CurrentMenu == menu) { return; }

        LastMenu = CurrentMenu;
		CurrentMenu = menu;

		topBarButtonsContainer.GetNode<Button>(new(LastMenu.Name)).Disabled = false;
		topBarButtonsContainer.GetNode<Button>(new(CurrentMenu.Name)).Disabled = true;

        double tweenTime = instant ? 0 : 0.15;

		Tween outTween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		outTween.TweenProperty(LastMenu, "modulate", Color.Color8(255, 255, 255, 0), tweenTime);
		outTween.TweenCallback(Callable.From(() => { LastMenu.Visible = false; }));

		CurrentMenu.Visible = true;

		Tween inTween = CreateTween().SetTrans(Tween.TransitionType.Quad);
		inTween.TweenProperty(CurrentMenu, "modulate", Color.Color8(255, 255, 255), tweenTime);
	}
}
