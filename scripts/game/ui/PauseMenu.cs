using System;
using Godot;

public partial class PauseMenu : Panel
{
    public bool Shown = false;

    [Export]
    private Game game;

    [Export]
    private Runner runner;

    [Export]
    private Button hide;

    [Export]
    private Button resume;

    [Export]
    private Button restart;

    [Export]
    private Button settings;

    [Export]
    private Button quit;

    public override void _Ready()
    {
        hide.Pressed += () => HideMenu();
        resume.Pressed += () => HideMenu();
        restart.Pressed += game.Restart;
        settings.Pressed += () =>
        {
            SettingsMenu.Instance.ShowMenu();
        };
        quit.Pressed += runner.GiveUp;
    }

    public void ShowMenu(bool show = true, bool instant = false)
    {
        Shown = show;
        runner.Playing = !Shown;

        var attempt = runner.Attempt;

        SoundManager.Song.PitchScale = (float)attempt.Speed;
        SoundManager.Song.StreamPaused = !runner.Playing;

        MenuCursor.Instance.UpdateVisible(
            Shown && SettingsManager.Instance.Settings.UseCursorInMenus.Value
        );

        if (Shown)
        {
            Visible = true;
            Input.WarpMouse(GetViewport().GetWindow().Size / 2);
        }
        else
        {
            // Re-sync the audio just in case
            // Already done when the map is running

            // double currentTime = Math.Max(0, (Attempt.Progress - Attempt.Settings.LocalOffset) / 1000.0f);

            // if (Attempt.Map.AudioBuffer != null && SoundManager.Song.Playing)
            // {
            // 	double desyncOffsetTime = Math.Abs(currentTime - SoundManager.Song.GetPlaybackPosition());

            // 	if (desyncOffsetTime > 0.05f)
            // 	{
            // 		Logger.Log($"[color=yellow]Desync detected! Offset by [b]{desyncOffsetTime:F5} seconds![/b][/color]");
            // 		SoundManager.Song.Seek((float)currentTime);
            // 	}
            // }

            Input.MouseMode =
                attempt.IsReplay && game.ReplayManager.ViewerVisible ? Input.MouseModeEnum.Visible
                : attempt.Settings.AbsoluteInput ? Input.MouseModeEnum.ConfinedHidden
                : Input.MouseModeEnum.Captured;
        }

        Tween tween = CreateTween();
        tween
            .TweenProperty(
                this,
                "modulate",
                Color.Color8(255, 255, 255, (byte)(Shown ? 255 : 0)),
                instant ? 0 : 0.25
            )
            .SetTrans(Tween.TransitionType.Quad);
        tween.TweenCallback(
            Callable.From(() =>
            {
                Visible = Shown;
            })
        );
        tween.Play();
    }

    public void HideMenu(bool instant = false)
    {
        ShowMenu(false, instant);
    }
}
