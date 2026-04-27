using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Godot;

public partial class StatsPanel : ExtrasPanel
{
    private bool initialized = false;
    private Label gamePlaytime { get; set; }
    private Label totalPlaytime { get; set; }
    private Label gamesOpened { get; set; }
    private Label totalDistance { get; set; }
    private Label notesHit { get; set; }
    private Label notesMissed { get; set; }
    private Label highestCombo { get; set; }
    private Label attempts { get; set; }
    private Label passes { get; set; }
    private Label fullCombos { get; set; }
    private Label highestScore { get; set; }
    private Label totalScore { get; set; }
    private Label averageAccuracy { get; set; }
    private Label rageQuits { get; set; }
    private Label favouriteMap { get; set; }

    public override void _Ready()
    {
        base._Ready();
        Node vbox = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");
        gamePlaytime = vbox.GetNode<Label>("GamePlaytime/Value");
        totalPlaytime = vbox.GetNode<Label>("TotalPlaytime/Value");
        gamesOpened = vbox.GetNode<Label>("GamesOpened/Value");
        totalDistance = vbox.GetNode<Label>("TotalDistance/Value");
        notesHit = vbox.GetNode<Label>("NotesHit/Value");
        notesMissed = vbox.GetNode<Label>("NotesMissed/Value");
        highestCombo = vbox.GetNode<Label>("HighestCombo/Value");
        attempts = vbox.GetNode<Label>("Attempts/Value");
        passes = vbox.GetNode<Label>("Passes/Value");
        fullCombos = vbox.GetNode<Label>("FullCombos/Value");
        highestScore = vbox.GetNode<Label>("HighestScore/Value");
        totalScore = vbox.GetNode<Label>("TotalScore/Value");
        averageAccuracy = vbox.GetNode<Label>("AverageAccuracy/Value");
        rageQuits = vbox.GetNode<Label>("RageQuits/Value");
        favouriteMap = vbox.GetNode<Label>("FavouriteMap/Value");
        applyStats(Stats.Instance);
        initialized = true;
    }

    public override void _EnterTree()
    {
        if (initialized)
        {
            applyStats(Stats.Instance);
            Stats.Instance.StatsUpdated += applyStats;
        }
    }

    public override void _ExitTree()
    {
        if (initialized)
        {
            Stats.Instance.StatsUpdated -= applyStats;
        }
        base._ExitTree();
    }

    private void applyStats(Stats stats)
    {
        gamePlaytime.Text = $"{stats.GamePlaytime / 3600}h";
        totalPlaytime.Text = $"{stats.TotalPlaytime / 3600}h";
        gamesOpened.Text = $"{stats.GamesOpened}";
        totalDistance.Text = $"{stats.TotalDistance / 1000}m";
        notesHit.Text = $"{stats.NotesHit}";
        notesMissed.Text = $"{stats.NotesMissed}";
        highestCombo.Text = $"{stats.HighestCombo}";
        attempts.Text = $"{stats.Attempts}";
        passes.Text = $"{stats.Passes}";
        fullCombos.Text = $"{stats.FullCombos}";
        highestScore.Text = $"{stats.HighestScore}";
        averageAccuracy.Text = $"{stats.AverageAccuracy:F2}";
        rageQuits.Text = $"{stats.RageQuits}";

        Map map = MapManager.Maps.Aggregate((m1, m2) => m1.PlayCount > m2.PlayCount ? m1 : m2);
        if (map.PlayCount > 0)
        {
            favouriteMap.Text = $"{map.Title}";
        }
    }

}
