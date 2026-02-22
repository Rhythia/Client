using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class Results : BaseScene
{
	private SettingsProfile settings;

	private static Panel footer;
	private static Panel holder;
	private static TextureRect cover;

	public static double LastFrame = 0;
	public static Vector2 MousePosition = Vector2.Zero;

	public override void _Ready()
	{
		base._Ready();
		
		settings = SettingsManager.Instance.Settings;

		footer = GetNode<Panel>("Footer");
		holder = GetNode<Panel>("Holder");
		cover = GetNode<TextureRect>("Cover");

		Input.MouseMode = settings.UseCursorInMenus ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        MenuCursor.Instance.Visible = settings.UseCursorInMenus;

        holder.GetNode<Label>("Title").Text = (LegacyRunner.CurrentAttempt.IsReplay ? "[REPLAY] " : "") + LegacyRunner.CurrentAttempt.Map.PrettyTitle;
		holder.GetNode<Label>("Difficulty").Text = LegacyRunner.CurrentAttempt.Map.DifficultyName;
		holder.GetNode<Label>("Mappers").Text = $"by {LegacyRunner.CurrentAttempt.Map.PrettyMappers}";
		holder.GetNode<Label>("Accuracy").Text = $"{LegacyRunner.CurrentAttempt.Accuracy.ToString().PadDecimals(2)}%";
		holder.GetNode<Label>("Score").Text = $"{Util.String.PadMagnitude(LegacyRunner.CurrentAttempt.Score.ToString())}";
		holder.GetNode<Label>("Hits").Text = $"{Util.String.PadMagnitude(LegacyRunner.CurrentAttempt.Hits.ToString())} / {Util.String.PadMagnitude(LegacyRunner.CurrentAttempt.Sum.ToString())}";
		holder.GetNode<Label>("Status").Text = LegacyRunner.CurrentAttempt.IsReplay ? LegacyRunner.CurrentAttempt.Replays[0].Status : LegacyRunner.CurrentAttempt.Alive ? (LegacyRunner.CurrentAttempt.Qualifies ? "PASSED" : "DISQUALIFIED") : "FAILED";
		holder.GetNode<Label>("Speed").Text = $"{LegacyRunner.CurrentAttempt.Speed.ToString().PadDecimals(2)}x";

		HBoxContainer modifiersContainer = holder.GetNode("Modifiers").GetNode<HBoxContainer>("HBoxContainer");
		TextureRect modTemplate = modifiersContainer.GetNode<TextureRect>("ModifierTemplate");

		foreach (KeyValuePair<string, bool> mod in LegacyRunner.CurrentAttempt.Mods)
		{
			if (mod.Value)
			{
				TextureRect icon = modTemplate.Duplicate() as TextureRect;

				icon.Visible = true;
				icon.Texture = Util.Misc.GetModIcon(mod.Key);

				modifiersContainer.AddChild(icon);
			}
		}

		if (LegacyRunner.CurrentAttempt.Map.CoverBuffer != null)
		{
		    Image img = Util.Misc.LoadImageFromBuffer(LegacyRunner.CurrentAttempt.Map.CoverBuffer);
		    if (img != null)
		    {
		        cover.Texture = ImageTexture.CreateFromImage(img);
		        GetNode<TextureRect>("CoverBackground").Texture = cover.Texture;
		    }
		}

		if (LegacyRunner.CurrentAttempt.Map.AudioBuffer != null)
		{
			if (!SoundManager.Song.Playing)
			{
				SoundManager.Song.Play();
			}
		}

		SoundManager.Song.PitchScale = (float)LegacyRunner.CurrentAttempt.Speed;

		if (!LegacyRunner.CurrentAttempt.Map.Ephemeral)
		{
			// SoundManager.JukeboxIndex = SoundManager.JukeboxQueueInverse[LegacyRunner.CurrentAttempt.Map.ID];
		}

		Button replayButton = footer.GetNode<Button>("Replay");

		footer.GetNode<Button>("Back").Pressed += Stop;
		footer.GetNode<Button>("Play").Pressed += Replay;
		replayButton.Visible = !LegacyRunner.CurrentAttempt.Map.Ephemeral;
		replayButton.Pressed += () =>
		{
			string path;

			if (LegacyRunner.CurrentAttempt.IsReplay)
			{
				path = $"{Constants.USER_FOLDER}/replays/{LegacyRunner.CurrentAttempt.Replays[0].ID}.phxr";
			}
			else
			{
				path = LegacyRunner.CurrentAttempt.ReplayFile.GetPath();
			}

			if (File.Exists(path))
			{
				Replay replay = new(path);
				SoundManager.Song.Stop();
				
				LegacyRunner.Play(MapParser.Decode(replay.MapFilePath), replay.Speed, replay.StartFrom, replay.Modifiers, null, [replay]);
			}
		};
	}

	public override void _Process(double delta)
	{
		ulong now = Time.GetTicksUsec();
		delta = (now - LastFrame) / 1000000;
		LastFrame = now;

		Vector2 size = GetViewport().GetVisibleRect().Size;

		holder.Position = holder.Position.Lerp((size / 2 - MousePosition) * (8 / size.Y), Math.Min(1, (float)delta * 16));
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed)
		{
			switch (eventKey.PhysicalKeycode)
			{
				case Key.Escape:
					Stop();
					break;
				case Key.Quoteleft:
					Replay();
					break;
			}
		}
		else if (@event is InputEventMouseMotion eventMouseMotion)
		{
			MousePosition = eventMouseMotion.Position;
		}
		else if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
		{
			switch (eventMouseButton.ButtonIndex)
			{
				case MouseButton.Xbutton1:
					Stop();
					break;
			}
		}
	}

	public override void Load()
	{
		base.Load();

		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Adaptive);
	}

	public void UpdateVolume()
	{
		SoundManager.Song.VolumeDb = -80 + 70 * (float)Math.Pow(settings.VolumeMusic.Value / 100, 0.1) * (float)Math.Pow(settings.VolumeMaster.Value / 100, 0.1);
	}

	public void Replay()
	{
		Map map = MapParser.Decode(LegacyRunner.CurrentAttempt.Map.FilePath);
		map.Ephemeral = LegacyRunner.CurrentAttempt.Map.Ephemeral;
		SoundManager.Song.Stop();
		
		LegacyRunner.Play(map, LegacyRunner.CurrentAttempt.Speed, LegacyRunner.CurrentAttempt.StartFrom, LegacyRunner.CurrentAttempt.Mods);
	}

	public void Stop()
	{
		SceneManager.Load("res://scenes/main_menu.tscn");
	}
}
