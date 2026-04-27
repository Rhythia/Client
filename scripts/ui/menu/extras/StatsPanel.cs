using Godot;
using System.Linq;

public partial class StatsPanel : ExtrasPanel
{
    public override void _Ready()
    {
        base._Ready();
        Stats.OnSaved += Update;
        Update();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Stats.OnSaved -= Update;
    }

    // populates all stat labels from the Stats class
    public void Update()
    {
        var vbox = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");

        // playtime is stored in seconds, convert to hours
        vbox.GetNode<Label>("GamePlaytime/Value").Text = $"{Stats.GamePlaytime / 3600}h";
        vbox.GetNode<Label>("TotalPlaytime/Value").Text = $"{Stats.TotalPlaytime / 3600}h";
        vbox.GetNode<Label>("GamesOpened/Value").Text = $"{Stats.GamesOpened}";

        // distance stored in milimeters, convert to meters
        vbox.GetNode<Label>("TotalDistance/Value").Text = $"{Stats.TotalDistance / 1000}m";
        vbox.GetNode<Label>("NotesHit/Value").Text = $"{Stats.NotesHit}";
        vbox.GetNode<Label>("NotesMissed/Value").Text = $"{Stats.NotesMissed}";
        vbox.GetNode<Label>("HighestCombo/Value").Text = $"{Stats.HighestCombo}";
        vbox.GetNode<Label>("Attempts/Value").Text = $"{Stats.Attempts}";
        vbox.GetNode<Label>("Passes/Value").Text = $"{Stats.Passes}";
        vbox.GetNode<Label>("FullCombos/Value").Text = $"{Stats.FullCombos}";
        vbox.GetNode<Label>("HighestScore/Value").Text = $"{Stats.HighestScore}";
        vbox.GetNode<Label>("TotalScore/Value").Text = $"{Stats.TotalScore}";
        vbox.GetNode<Label>("AverageAccuracy/Value").Text = Stats.PassAccuracies.Count > 0
            ? $"{Stats.PassAccuracies.ToArray().Average().ToString().PadDecimals(2)}%"
            : "0%";
        vbox.GetNode<Label>("RageQuits/Value").Text = $"{Stats.RageQuits}";
        vbox.GetNode<Label>("FavouriteMap/Value").Text = Stats.FavoriteMaps.Count > 0
            ? Stats.FavoriteMaps.ToArray().OrderByDescending(x => x.Value).First().Key
            : "";
    }
}
