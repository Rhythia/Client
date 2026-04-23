using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class GameScene : BaseScene
{
	[Export] public Runner Runner;
	[Export] public Panel Menu;
	[Export] public ReplayManager ReplayManager;
	public static Attempt Attempt;

	public bool MenuShown = false;

	public static GameScene Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

	public override void _ExitTree()
    {
        Instance.QueueFree();
    }

	public override void _Ready()
	{
		base._Ready();

		Control focused = SceneManager.Root.GetViewport().GuiGetFocusOwner();
		focused?.ReleaseFocus();
		Input.MouseMode = Attempt.Settings.AbsoluteInput.Value || Attempt.IsReplay ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured;
		Input.UseAccumulatedInput = false;

		Panel menuButtonsHolder = Menu.GetNode<Panel>("Holder");

		Menu.GetNode<Button>("Button").Pressed += HideMenu;
		menuButtonsHolder.GetNode<Button>("Resume").Pressed += HideMenu;
		menuButtonsHolder.GetNode<Button>("Restart").Pressed += Restart;
		menuButtonsHolder.GetNode<Button>("Settings").Pressed += () => {
			SettingsManager.ShowMenu();
		};
		menuButtonsHolder.GetNode<Button>("Quit").Pressed += () => {
			if (Attempt.Alive)
			{
				SoundManager.FailSound.Play();
			}

			Attempt.Alive = false;
			Attempt.Qualifies = false;

			if (Attempt.DeathTime == -1)
			{
				Attempt.DeathTime = Math.Max(0, Attempt.Progress);
			}

			Runner.Stop();
		};
		
		Runner.Attempt = Attempt;
		ReplayManager.InitReplayLength();

		if (Runner.Attempt.IsReplay)
		{
			GD.Print("Replay Mode: playback");
			ReplayManager.CurrentMode = ReplayManager.Mode.PLAYBACK;
		}
		else if (Runner.Attempt.Settings.RecordReplays)
		{
			ReplayManager.NewReplay(Runner.Attempt);
			GD.Print("Replay Mode: record");
			ReplayManager.CurrentMode = ReplayManager.Mode.RECORD;
		}
		else
		{
			GD.Print("Replay Mode: none");
			ReplayManager.CurrentMode = ReplayManager.Mode.NONE;
		}
		
    	Runner.Play();
	}

    public override void Load()
    {
        base.Load();

		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

		MenuCursor.Instance.UpdateVisible(false, false);
        SceneManager.Space.UpdateState(true);
		SceneManager.Space.UpdateMap(Attempt.Map);
    }

	public static void Play(Map map, double speed, double startFrom, Dictionary<string, bool> mods, string[] players = null, Replay[] replays = null)
	{
		map = MapParser.Decode(map.FilePath);
		Attempt = new Attempt(map, speed, startFrom, mods ?? [], players, replays);
		SceneManager.Load("res://scenes/game.tscn");
	}
	
	public void Restart()
	{
		Attempt.Alive = false;
		Attempt.Qualifies = false;
		Runner.Stop(false);

		var oldAttempt = Attempt;
		var map = MapParser.Decode(oldAttempt.Map.FilePath);
		Attempt = new Attempt(map, oldAttempt.Speed, oldAttempt.StartFrom, oldAttempt.Mods, oldAttempt.Players, oldAttempt.Replays);

		SceneManager.ReloadCurrentScene();
	}

	public override void _Process(double delta)
	{
		if (ReplayManager.CurrentMode == ReplayManager.Mode.PLAYBACK)
		{
			ReplayManager.UpdateReplayCursor(Attempt);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			if (!Runner.Playing || Attempt.IsReplay) return;

			if (!Attempt.Settings.AbsoluteInput)
			{
				UpdateCursor(eventMouseMotion.Relative);
			}
			else
			{
				// Take mouse position difference between center of the current window size
				// This is to make the mouse position the same as relative if it was locked, or confined
				Vector2 AbsolutePosition = eventMouseMotion.Position - (GetViewport().GetWindow().Size / 2);

				// Multiply by 0.582f to make it 1:1 to absolute scale on nightly
				UpdateCursor(AbsolutePosition * 0.582f);
			}

			Attempt.DistanceMM += eventMouseMotion.Relative.Length() / Attempt.Settings.Sensitivity / 57.5;
		}
		else if (@event is InputEventKey eventKey && eventKey.Pressed)
		{
			switch (eventKey.PhysicalKeycode)
			{
				case Key.Escape:
					Attempt.Qualifies = false;

					if (SettingsManager.Shown)
					{
						SettingsManager.HideMenu();
					}
					else
					{
						ShowMenu(!MenuShown);
					}

					break;
				case Key.Quoteleft:
					Restart();
					break;
				case Key.F1:
					if (Attempt.IsReplay)
					{
						ReplayManager.ShowReplayViewer(Attempt);
					}
					break;
				case Key.Space:
					if (Attempt.IsReplay)
					{
						Runner.Playing = !Runner.Playing;
						SoundManager.Song.PitchScale = (float)Attempt.Speed;
						SoundManager.Song.StreamPaused = !Runner.Playing;

						string texturePath = Runner.Playing ? "res://textures/ui/pause.png" : "res://textures/ui/play.png";
						ReplayManager.SeekerPause.TextureNormal = GD.Load<Texture2D>(texturePath);
					}
					else
					{
						if (Lobby.Players.Count > 1)
						{
							break;
						}

						Runner.Skip();
					}
					break;
				case Key.F:
					Attempt.Settings.FadeOut.Value = !Attempt.Settings.FadeOut;
					break;
				case Key.P:
					Attempt.Settings.Pushback.Value = !Attempt.Settings.Pushback;
					break;
			}
		}
		else if (@event is InputEventMouseButton eventMouseButton)
		{
			switch (eventMouseButton.ButtonIndex)
			{
				case MouseButton.Left:
					ReplayManager.LMB = eventMouseButton.Pressed;
					break;
			}
		}
	}

    public void UpdateCursor(Vector2 mouseDelta)
	{
		float sensitivity = (float)(Attempt.IsReplay ? Attempt.Replays[0].Sensitivity : Attempt.Settings.Sensitivity);
		sensitivity *= (float)Attempt.Settings.FoV.Value / 70f;

		if (Attempt.Settings.AbsoluteInput)
		{
			// Reset everything to zero so it doesn't spin endlessly, or have infinite sensitivity
			Runner.Camera.Rotation = Vector3.Zero;
			Attempt.RawCursorPosition = Vector2.Zero;
			Attempt.CursorPosition = Vector2.Zero;
		}

		if (!Runner.SpinCamera)
		{
			if (Attempt.Settings.CursorDrift)
			{
				Attempt.CursorPosition = (Attempt.CursorPosition + new Vector2(1, -1) * mouseDelta / 120 * sensitivity).Clamp(-Constants.BOUNDS, Constants.BOUNDS);
			}
			else
			{
				Attempt.RawCursorPosition += new Vector2(1, -1) * (mouseDelta * sensitivity / 120f);
				Attempt.CursorPosition = Attempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);
			}

			Runner.Cursor.Position = new Vector3(Attempt.CursorPosition.X, Attempt.CursorPosition.Y, 0);
			Runner.Camera.Position = new Vector3(0, 0, 3.75f) + new Vector3(Attempt.CursorPosition.X, Attempt.CursorPosition.Y, 0) * (float)Attempt.Settings.CameraParallax;
			Runner.Camera.Rotation = Vector3.Zero;

			//videoQuad.Position = new Vector3(Camera.Position.X, Camera.Position.Y, -100);
		}
		else
		{
			Runner.Camera.Rotation += new Vector3(-mouseDelta.Y / 120 * sensitivity / (float)Math.PI, -mouseDelta.X / 120 * sensitivity / (float)Math.PI, 0);

			Runner.Camera.Rotation = new Vector3((float)Math.Clamp(Runner.Camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)), Runner.Camera.Rotation.Y, Runner.Camera.Rotation.Z);

			Vector3 Origin = new Vector3(0,0,3.5f);
			Vector3 CursorLock = new Vector3(Attempt.CursorPosition.X, Attempt.CursorPosition.Y, 0);
			// The pivot is to mimic ROBLOX's orbital camera
			Vector3 Pivot = Runner.Camera.Basis.Z / 4f;

			Runner.Camera.Position = Origin + CursorLock * Attempt.Settings.CameraParallax + Pivot;

			Vector3 LookVector = Runner.Camera.Basis.Z;
			Vector2 CameraVec2 = new Vector2(Runner.Camera.Position.X, Runner.Camera.Position.Y);
			Vector2 LookVec2 = new Vector2(LookVector.X, LookVector.Y);

			Attempt.RawCursorPosition = CameraVec2 - LookVec2 * Mathf.Abs(Runner.Camera.Position.Z / LookVector.Z);

			Attempt.CursorPosition = Attempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);
			Runner.Cursor.Position = new Vector3(Attempt.CursorPosition.X, Attempt.CursorPosition.Y, 0);

			//videoQuad.Position = Camera.Position - Camera.Basis.Z * 103.75f;
			//videoQuad.Rotation = Camera.Rotation;
		}
    }

	public void ShowMenu(bool show = true)
	{
		MenuShown = show;
		Runner.Playing = !MenuShown;
		
		// rest in peace 0.000000000000000001f pitch scale -fog
		SoundManager.Song.PitchScale = (float)Attempt.Speed;
		SoundManager.Song.StreamPaused = !Runner.Playing;

		MenuCursor.Instance.UpdateVisible(MenuShown && SettingsManager.Instance.Settings.UseCursorInMenus.Value);

		if (MenuShown)
		{
			Menu.Visible = true;
			Input.WarpMouse(GetViewport().GetWindow().Size / 2);
		}
		else
		{
			Input.MouseMode = Attempt.Settings.AbsoluteInput || Attempt.IsReplay ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured;
		}

		Tween tween = Menu.CreateTween();
		tween.TweenProperty(Menu, "modulate", Color.Color8(255, 255, 255, (byte)(MenuShown ? 255 : 0)), 0.25).SetTrans(Tween.TransitionType.Quad);
		tween.TweenCallback(Callable.From(() => {
			Menu.Visible = MenuShown;
		}));
		tween.Play();
	}

	public void HideMenu()
	{
		ShowMenu(false);
	}
}
