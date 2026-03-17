using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public partial class ScorePanel : Panel
{
    public Leaderboard.Score Score;

    public Button Button;

    private Label playerLabel;
    private Label scoreLabel;
    private Label accuracyLabel;
    private Label speedLabel;
    private Label timeLabel;
    private HBoxContainer modifiers;
    private TextureRect modifierTemplate;

    public override void _Ready()
    {
        Button = GetNode<Button>("Button");

        playerLabel = GetNode<Label>("Player");
        scoreLabel = GetNode<Label>("Score");
        accuracyLabel = GetNode<Label>("Accuracy");
        speedLabel = GetNode<Label>("Speed");
        timeLabel = GetNode<Label>("Time");
        modifiers = GetNode<HBoxContainer>("Modifiers");
        modifierTemplate = modifiers.GetNode<TextureRect>("ModifierTemplate");

        Panel buttonHover = Button.GetNode<Panel>("Hover");
        Label buttonLabel = buttonHover.GetNode<Label>("Label");

        void tweenHover(bool show)
        {
            string replayPath = $"{Constants.USER_FOLDER}/replays/{Score.AttemptID}.phxr";

            buttonLabel.Text = File.Exists(replayPath) ? "VIEW" : "REPLAY NOT FOUND";

            CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quart).TweenProperty(buttonHover, "modulate", Color.Color8(255, 255, 255, (byte)(show ? 255 : 0)), 0.25);
        }

        Button.MouseEntered += () => { tweenHover(true); };
        Button.MouseExited += () => { tweenHover(false); };
        Button.Pressed += Replay;
    }

    public void Setup(Leaderboard.Score score)
    {
        Score = score;

        playerLabel.Text = score.Player;
        accuracyLabel.Text = $"{score.Accuracy:F2}%";
        speedLabel.Text = $"{score.Speed:F2}x";
        timeLabel.Text = Util.String.FormatUnixTimePretty(Time.GetUnixTimeFromSystem(), score.Time);

        if (score.Qualifies)
        {
            scoreLabel.Text = Util.String.PadMagnitude(score.Value.ToString());
        }
        else
        {
            scoreLabel.Text = $"{Util.String.FormatTime(Math.Max(0, score.Progress) / 1000)} / {Util.String.FormatTime(score.MapLength / 1000)}";
            scoreLabel.LabelSettings = scoreLabel.LabelSettings.Duplicate() as LabelSettings;
            scoreLabel.LabelSettings.FontColor = Color.Color8(160, 160, 160);
        }

        foreach (KeyValuePair<string, bool> entry in score.Modifiers)
        {
            if (entry.Value)
            {
                TextureRect mod = modifierTemplate.Duplicate() as TextureRect;
                mod.Texture = Util.Misc.GetModIcon(entry.Key);
                mod.Visible = true;
                modifiers.AddChild(mod);
            }
        }
    }

    public void Replay()
    {
        string replayPath = $"{Constants.USER_FOLDER}/replays/{Score.AttemptID}.phxr";

        if (File.Exists(replayPath))
        {
            Replay replay = new(replayPath);
            Map map = MapParser.Decode(replay.MapFilePath);

            LegacyRunner.Play(map, replay.Speed, replay.StartFrom, replay.Modifiers, null, [replay]);
        }
    }
}
