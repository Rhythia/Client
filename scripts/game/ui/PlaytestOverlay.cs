using System;
using System.Globalization;
using Godot;

public partial class PlaytestOverlay : Panel
{
    [Export]
    public ReplayManager ReplayManager { get; set; }

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
            Input.MouseMode =
                Attempt.IsReplay && ReplayManager.ViewerVisible ? Input.MouseModeEnum.Visible
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
            double sfInit = util.Misc.ParseDoubleInput(Rhythia.StartFromParameter);
            string sfSeconds = (sfInit /= 1000).ToString(CultureInfo.InvariantCulture);
            ApplyStartFrom(sfSeconds, Attempt.Map, startFromEdit);

            // speed init
            double spInit = util.Misc.ParseDoubleInput(Rhythia.SpeedParameter, 1.0);
            speedEdit.Text = spInit.ToString(CultureInfo.InvariantCulture);
        }

        if (!show)
        {
            var oldAttempt = Attempt;
            Runner.Stop(false);

            double speedValue = util.Misc.ParseDoubleInput(speedEdit.Text, 1.0);

            var map = MapParser.Decode(oldAttempt.Map.FolderPath, Rhythia.AudioFilePath);

            Game.Attempt = new(map, speedValue, GetStartFrom(startFromEdit) * 1000, Rhythia.TempCam, Rhythia.TempMods);
            SceneManager.ReloadCurrentScene();
        }
    }

    public static void ApplyStartFrom(string input, Map map, LineEdit valueEdit)
    {
        // Hello MapInfoContainer.cs! :) -fog

        input ??= valueEdit.Text == "" ? valueEdit.PlaceholderText : valueEdit.Text;

        if (input.Contains(':')) // time conversion (ex. 1:25)
        {
            if (!util.Misc.TryParseDoubleInput(input.Replace(":", ""), out _))
            {
                valueEdit.Text = util.String.FormatTime(1.0);
            }

            double value = 0;
            string[] split = input.Split(":");
            split.Reverse();

            if (split.Length > 1 && util.Misc.TryParseDoubleInput(split[1], out double minutes))
            {
                value += 60 * minutes;
            }
            if (util.Misc.TryParseDoubleInput(split[0], out double inputValue))
            {
                if (inputValue < 1)
                    inputValue *= map.Length / 1000;
                value += inputValue;
            }

            value = Math.Clamp(value * 1000, 0, map.Length);

            valueEdit.Text = util.String.FormatTime(value / 1000);
        }
        else if (!input.Contains(':'))
        {
            if (!util.Misc.TryParseDoubleInput(input, out _))
            {
                valueEdit.Text = util.String.FormatTime(1.0);
            }

            double value = util.Misc.ParseDoubleInput(input);
            value = Math.Clamp(value, 0, map.Length);

            valueEdit.Text = util.String.FormatTime(value);
        }
    }

    public static double GetStartFrom(LineEdit valueEdit)
    {
        double value = 0;
        string input = valueEdit.Text;

        string[] split = input.Split(":");

        if (split.Length == 1)
        {
            value = util.Misc.ParseDoubleInput(split[0]);
        }
        else
        {
            if (util.Misc.TryParseDoubleInput(split[0], out double minutes))
            {
                value += minutes * 60;
            }

            if (util.Misc.TryParseDoubleInput(split[1], out double seconds))
            {
                value += seconds;
            }
        }

        return value;
    }
}
