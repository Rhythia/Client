using System;
using Godot;

public partial class PlaytestOverlay : Panel
{
    [Export] public ReplayManager ReplayManager { get; set; }

    public bool PlaytestInit = false;
    public Runner Runner;
    public Attempt Attempt;

    public override void _Ready()
    {
        var holder = GetNode("VB");
        var startFromEdit = holder.GetNode<LineEdit>("StartFrom/LineEdit");

        startFromEdit.FocusExited += () => ApplyStartFrom(startFromEdit.Text, Attempt.Map, startFromEdit);
        startFromEdit.TextSubmitted += _ => ApplyStartFrom(startFromEdit.Text, Attempt.Map, startFromEdit);
        holder.GetNode<Button>("PlayButton").Pressed += () => UpdatePlaytestOverlay(false);
    }

    public void UpdatePlaytestOverlay(bool show)
    {
        Runner.Playing = !show;

        SoundManager.Song.PitchScale = (float)Attempt.Speed;
        SoundManager.Song.StreamPaused = !Runner.Playing;

        Visible = show;

        MenuCursor.Instance.UpdateVisible(Visible && SettingsManager.Instance.Settings.UseCursorInMenus.Value);

        if (Visible)
        {
            Input.WarpMouse(GetViewport().GetWindow().Size / 2);
        }
        else
        {
            Input.MouseMode = Attempt.IsReplay && ReplayManager.ViewerVisible ? Input.MouseModeEnum.Visible
                : Attempt.Settings.AbsoluteInput ? Input.MouseModeEnum.ConfinedHidden
                : Input.MouseModeEnum.Captured;
        }

        var holder = GetNode("VB");

        Rhythia.TempCam = holder.GetNode<CheckButton>("SpinCheck").ButtonPressed ? new CameraSpin() : new CameraLock();

        var startFromEdit = holder.GetNode<LineEdit>("StartFrom/LineEdit");
        var speedEdit = holder.GetNode<LineEdit>("Speed/LineEdit");

        PlaytestInit = !show;

        if (!PlaytestInit && SettingsManager.Instance.Settings.OptionalPlaytestParameters)
        {
            // start from init
            double.TryParse(Rhythia.StartFromParameter, out double sfInit);
            string sfSeconds = (sfInit /= 1000).ToString();
            ApplyStartFrom(sfSeconds, Attempt.Map, startFromEdit);

            // speed init
            double.TryParse(Rhythia.SpeedParameter, out double spInit);
            speedEdit.Text = spInit.ToString();
        }

        if (!show)
        {
            var oldAttempt = Attempt;
            Runner.Stop(false);

            double speedValue = 1.0;

            if (double.TryParse(speedEdit.Text, out double speedDouble))
            {
                speedValue = speedDouble;
            }

            var map = MapParser.Decode(oldAttempt.Map.FolderPath, Rhythia.AudioFilePath);

            Game.Attempt = new(map, speedValue, GetStartFrom(startFromEdit) * 1000, Rhythia.TempCam, Rhythia.TempMods);
            SceneManager.ReloadCurrentScene();
        }
    }

    public void ApplyStartFrom(string input, Map map, LineEdit valueEdit)
    {
        // Hello MapInfoContainer.cs! :) -fog

        input ??= valueEdit.Text == "" ? valueEdit.PlaceholderText : valueEdit.Text;


        if (input.Contains(":")) // time conversion (ex. 1:25)
        {
            if (!input.IsValidFloat())
            {
                valueEdit.Text = Util.String.FormatTime(1.0);
            }

            double value = 0;
            string[] split = input.Split(":");
            split.Reverse();

            if (split.Length > 1 && split[1].IsValidFloat())
            {
                value += 60 * split[1].ToFloat();
            }
            if (double.TryParse(split[0], System.Globalization.CultureInfo.InvariantCulture, out double inputValue))
            {
                if (inputValue < 1) inputValue *= map.Length / 1000;
                value += inputValue;
            }

            value = Math.Clamp(value * 1000, 0, map.Length);

            valueEdit.Text = Util.String.FormatTime(value / 1000);
        }
        else if (!input.Contains(":"))
        {
            if (!input.IsValidFloat())
            {
                valueEdit.Text = Util.String.FormatTime(1.0);
            }

            double value = 0.0;

            if (double.TryParse(input, out double inputValue)) value = inputValue;
            value = Math.Clamp(value, 0, map.Length);

            valueEdit.Text = Util.String.FormatTime(value);
        }
    }

    public double GetStartFrom(LineEdit valueEdit)
    {
        double value = 0;
        string input = valueEdit.Text;

        string[] split = input.Split(":");

        if (split.Length == 1)
        {
            if (split[0].IsValidFloat()) value = split[0].ToFloat();

        }
        else
        {
            if (split[0].IsValidFloat())
            {
                value += split[0].ToFloat() * 60; // minutes
            }

            if (split[1].IsValidFloat())
            {
                value += split[1].ToFloat(); // seconds
            }
        }

        return value;
    }
}
