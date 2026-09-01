using System.Collections.Generic;
using Godot;

public partial class Game : BaseScene
{
    [Export] public Runner Runner;
    [Export] public PauseMenu Menu;
    [Export] public PlaytestOverlay PlaytestOverlay;
    [Export] public ReplayManager ReplayManager { get; private set; }
    [Export] public PlayerInputController PlayerInputController { get; private set; }
    [Export] public CursorManager CursorManager { get; private set; }

    [Signal] public delegate void StartTempPauseEventHandler(Attempt attempt);

    public static Game Instance;
    public static Attempt Attempt;
    public static bool StartQueued = false;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        ReplayManager ??= GetNode<ReplayManager>("ReplayManager");
        CursorManager ??= GetNode<CursorManager>("CursorManager");
        PlayerInputController ??= GetNode<PlayerInputController>("PlayerInputController");

        if (CursorManager == null)
            Logger.Error("No CursorManager found!");
        if (PlayerInputController == null)
            Logger.Error("No PlayerInputController found!");

        PlayerInputController.OnMouseMove += (relative, absolute) =>
        {
            if (!Runner.Playing || Attempt.IsReplay) return;

            if (Attempt.Settings.AbsoluteInput)
            {
                // Take mouse position difference between center of the current window size
                // This is to make the mouse position the same as relative if it was locked, or confined
                Vector2 absolutePosition = absolute - (GetViewport().GetWindow().Size / 2);

                // Multiply by 0.582f to make it 1:1 to absolute scale on nightly
                CursorManager.UpdateCursor(absolutePosition * 0.582f);
            }
            else
            {
                CursorManager.UpdateCursor(relative);
            }
            Attempt.DistanceMM += relative.Length() / Attempt.Settings.Sensitivity / 57.5;
        };

        PlayerInputController.OnLeftMouseButton += isPressed =>
        {

        };

        PlayerInputController.OnTogglePaused += () =>
        {
            if (Runner.ObjectIndicesStart[typeof(Note)] > 0 && Attempt.Progress < Attempt.Map.Notes[^1].Millisecond)
            {
                Attempt.Qualifies = false;
            }

            if (SettingsManager.Shown)
            {
                SettingsMenu.Instance.HideMenu();
            }
            else
            {
                if (Rhythia.TempMode && !PlaytestOverlay.PlaytestInit) return;
                Menu.ShowMenu(!Menu.Shown);
            }
        };

        PlayerInputController.OnToggleReplayViewerVisibility += () =>
        {
            if (Attempt.IsReplay)
            {
                ReplayManager.ShowReplayViewer(Attempt);
            }
        };

        PlayerInputController.OnToggleShowOrthonogalCamera += () =>
        {
            if (Attempt.IsReplay)
            {
                ReplayManager.ShowOrthonogalCamera(Attempt);
            }
        };

        PlayerInputController.OnPauseOrSkipPressed += () =>
        {
            if (Attempt.IsReplay)
            {
                ReplayManager.TogglePause();
            }
            else if (PlaytestOverlay.PlaytestInit == false && Rhythia.TempMode)
            {
                PlaytestOverlay.UpdatePlaytestOverlay(false);
            }
            else
            {
                if (Lobby.Players.Count > 1) return;
                Runner.Skip();

                // Space To Pause
                // if (!Attempt.CanSkip && Attempt.Settings.SpaceToPause)
                // {
                // 	EmitSignal(SignalName.StartTempPause, Attempt);
                // }
            }
        };

        // PlayerInputController.OnPauseOrSkipReleased += () =>
        // {

        // };

        PlayerInputController.OnToggleFade += () => Attempt.Settings.FadeOut.Value = Attempt.Settings.FadeOut > 0 ? 0 : 100;
        PlayerInputController.OnTogglePushback += () => Attempt.Settings.Pushback.Value = !Attempt.Settings.Pushback;
        PlayerInputController.OnRestartPressed += Restart;
    }

    public override void Load()
    {
        base.Load();

        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

        MenuCursor.Instance.UpdateVisible(false, false);
        SceneManager.Space.UpdateState(true);
        SceneManager.Space.UpdateMap(Attempt.Map);

        var focused = SceneManager.Root.GetViewport().GuiGetFocusOwner();
        focused?.ReleaseFocus();

        Input.MouseMode = Attempt.Settings.AbsoluteInput || Attempt.IsReplay ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        Input.UseAccumulatedInput = false;

        Runner.Attempt = Attempt;
        ReplayManager.InitReplayLength();
        ReplayManager.ShowReplayViewer(Runner.Attempt, Runner.Attempt.IsReplay);
        ReplayManager.ShowOrthonogalCamera(Runner.Attempt, Runner.Attempt.IsReplay);

        Menu.HideMenu(true);

        if (Runner.Attempt.IsReplay)
        {
            ReplayManager.CurrentMode = ReplayManager.Mode.PLAYBACK;
        }
        else if (Runner.Attempt.Settings.RecordReplays)
        {
            ReplayManager.NewReplay(Runner.Attempt);
            ReplayManager.CurrentMode = ReplayManager.Mode.RECORD;
        }
        else
        {
            ReplayManager.CurrentMode = ReplayManager.Mode.NONE;
        }

        Logger.Log($"Replay Mode: {ReplayManager.CurrentMode}");

        Runner.Play();
        StartQueued = false;

        if (!Attempt.IsReplay)
        {
            Stats.Instance.Attempts++;
            Attempt.Map.PlayCount++;
            MapManager.Update(Attempt.Map);
        }

        if (Rhythia.TempMode && !PlaytestOverlay.PlaytestInit)
        {
            PlaytestOverlay.Attempt = Attempt;
            PlaytestOverlay.Runner = Runner;
            PlaytestOverlay.UpdatePlaytestOverlay(true);
        }
    }

    public static void Play(Map map, double speed, double startFrom, CameraMode cameraMode, List<Modifier> mods, string[] players = null, Replay[] replays = null)
    {
        if (StartQueued) return;

        StartQueued = true;

        var parsedMap = MapParser.Decode(map.FolderPath, Rhythia.AudioFilePath);
        Attempt = new(parsedMap, speed, startFrom, cameraMode, mods, players, replays);

        SceneManager.Load("res://scenes/game.tscn");
    }

    public void Restart()
    {
        Runner.Fail();
        Runner.Stop(false);

        var oldAttempt = Attempt;
        var map = MapParser.Decode(oldAttempt.Map.FolderPath, Rhythia.AudioFilePath);
        Attempt = new(map, oldAttempt.Speed, oldAttempt.StartFrom, oldAttempt.CameraMode, oldAttempt.Modifiers, oldAttempt.Players, oldAttempt.Replays);

        SceneManager.ReloadCurrentScene();
    }
}
