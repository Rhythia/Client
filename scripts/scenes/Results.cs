using System;
using System.Collections.Generic;
using Godot;

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

        // stops menu music after going to results scene
        SoundManager.MenuMusic?.Stop();

        Input.MouseMode = settings.UseCursorInMenus ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
        MenuCursor.Instance.Visible = settings.UseCursorInMenus;

        var attempt = Game.Attempt;

        holder.GetNode<Label>("Title").Text = (attempt.IsReplay ? "[REPLAY] " : "") + attempt.Map.PrettyTitle;
        holder.GetNode<Label>("Difficulty").Text = attempt.Map.DifficultyName;
        holder.GetNode<Label>("Mappers").Text = $"by {attempt.Map.PrettyMappers}";
        holder.GetNode<Label>("Accuracy").Text = $"{attempt.Accuracy:F2}%";
        holder.GetNode<Label>("Score").Text = $"{Util.String.PadMagnitude(attempt.Score.ToString())}";
        holder.GetNode<Label>("Hits").Text = $"{Util.String.PadMagnitude(attempt.Hits.ToString())} / {Util.String.PadMagnitude(attempt.Sum.ToString())}";
        holder.GetNode<Label>("Status").Text = attempt.IsReplay ? attempt.Replays[0].Status : attempt.Alive ? (attempt.Qualifies ? "PASSED" : "DISQUALIFIED") : "FAILED";
        holder.GetNode<Label>("Speed").Text = $"{attempt.Speed:F2}x";

        HBoxContainer modifiersContainer = holder.GetNode("Modifiers").GetNode<HBoxContainer>("HBoxContainer");
        TextureRect modTemplate = modifiersContainer.GetNode<TextureRect>("ModifierTemplate");

        foreach (var mod in attempt.Modifiers)
        {
            TextureRect icon = modTemplate.Duplicate() as TextureRect;

            icon.Visible = true;
            icon.Texture = Util.Misc.GetModIcon(mod.Name);

            modifiersContainer.AddChild(icon);
        }

        if (attempt.Map.CoverBuffer != null)
        {
            Image img = Util.Misc.LoadImageFromBuffer(attempt.Map.CoverBuffer);
            if (img != null)
            {
                cover.Texture = ImageTexture.CreateFromImage(img);
                GetNode<TextureRect>("CoverBackground").Texture = cover.Texture;
            }
        }

        // if (SettingsManager.Instance.Settings.AutoplayJukebox.Value && LegacyRunner.CurrentAttempt.Map.AudioBuffer != null)

        if (attempt.Map.AudioBuffer != null)
        {
            if (!SoundManager.Song.Playing)
            {
                SoundManager.Song.Play();
            }
        }

        SoundManager.Song.PitchScale = (float)attempt.Speed;

        if (!attempt.Map.Ephemeral)
        {
            // SoundManager.JukeboxIndex = SoundManager.JukeboxQueueInverse[attempt.Map.ID];
        }

        Button replayButton = footer.GetNode<Button>("Replay");

        footer.GetNode<Button>("Back").Pressed += Stop;
        footer.GetNode<Button>("Play").Pressed += Replay;
        replayButton.Visible = !attempt.Map.Ephemeral;

        if (!FileAccess.FileExists(attempt.ReplayPath) && !attempt.IsReplay)
        {
            // This can be multiple reasons, so I am just going to disable the notification
            // _ = ToastNotification.Notify("Replay desync detected! Sum didn't match notes hit", 2);
            replayButton.Visible = false;
        }

        replayButton.Pressed += () =>
        {
            string path;

            if (attempt.IsReplay)
            {
                path = $"{Constants.USER_FOLDER}/replays/{attempt.Replays[0].ID}.phxr";
            }
            else
            {
                path = attempt.ReplayPath;
            }

            if (FileAccess.FileExists(path))
            {
                Replay replay = new(path);
                SoundManager.Song.Stop();

                Game.Play(MapParser.Decode(replay.MapFilePath), replay.Speed, replay.StartFrom, replay.CameraMode, replay.Modifiers, null, [replay]);
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

        DisplayServer.WindowSetVsyncMode(SettingsManager.Instance.Settings.VSyncMenus ? DisplayServer.VSyncMode.Adaptive : DisplayServer.VSyncMode.Disabled);
    }

    public void UpdateVolume()
    {
        // SoundManager.Song.VolumeDb = (float)SoundManager.ComputeVolumeDb((float)settings.VolumeMusic.Value, (float)settings.VolumeMaster.Value, 70);
        SoundManager.Song.VolumeDb = -80 + 70 * (float)Math.Pow(settings.VolumeMusic.Value / 100, 0.1) * (float)Math.Pow(settings.VolumeMaster.Value / 100, 0.1);
    }

    public void Replay()
    {
        var attempt = Game.Attempt;

        Map map = MapParser.Decode(attempt.Map.FolderPath);
        map.Ephemeral = attempt.Map.Ephemeral;
        SoundManager.Song.Stop();

        Game.Play(map, attempt.Speed, attempt.StartFrom, attempt.CameraMode, attempt.Modifiers);
    }

    public void Stop()
    {
        if (Rhythia.TempMode)
        {
            Rhythia.Quit();
        }
        else
        {
            SceneManager.Load("res://scenes/main_menu.tscn");
        }

        //     if (!SettingsManager.Instance.Settings.AutoplayJukebox.Value)
        //     {
        //         SoundManager.StopScopedSession();
        //     }

        //     SoundManager.Song.PitchScale = (float)Lobby.Speed;
        //     SoundManager.UpdateVolume();
    }
}
