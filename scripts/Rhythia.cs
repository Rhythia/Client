using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class Rhythia : Node
{
    private static bool loaded = false;

    [Signal]
    public delegate void FilesDroppedEventHandler(string[] files);

    public static Rhythia Instance;
    public static bool Quitting { get; private set; } = false;

    public override async void _Ready()
    {
        Instance = this;

        GetTree().AutoAcceptQuit = false;

        // Settings

        if (!File.Exists($"{Constants.USER_FOLDER}/profiles/default.json"))
        {
            SettingsManager.Save("default");
        }

        try
        {
            SettingsManager.Load();
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            SettingsManager.Save();
        }

        // Stats

        if (!File.Exists($"{Constants.USER_FOLDER}/stats"))
        {
            Logger.Log("Stats file not found");
            File.WriteAllText($"{Constants.USER_FOLDER}/stats", "");
            Stats.Save();
        }

        try
        {
            Stats.Load();
        }
        catch
        {
            Stats.GamePlaytime = 0;
            Stats.TotalPlaytime = 0;
            Stats.GamesOpened = 0;
            Stats.TotalDistance = 0;
            Stats.NotesHit = 0;
            Stats.NotesMissed = 0;
            Stats.HighestCombo = 0;
            Stats.Attempts = 0;
            Stats.Passes = 0;
            Stats.FullCombos = 0;
            Stats.HighestScore = 0;
            Stats.TotalScore = 0;
            Stats.RageQuits = 0;
            Stats.PassAccuracies = [];
            Stats.FavoriteMaps = [];

            Stats.Save();
        }

        Stats.GamesOpened++;

        
        // marking sspms for importing can be done with an one liner, kept the following block of code in case we need to loop over every valid map file for some reason

        // List<string> import = [];
        // HashSet<string> validExtensions = new HashSet<string> { "phxm", "sspm", "txt" };
        // var files = Directory.EnumerateFiles($"{Constants.USER_FOLDER}/maps", $"*.*", SearchOption.AllDirectories).Where(f => validExtensions.Contains(f.GetExtension().ToLower()));

        // foreach (string file in files)
        // {
        //     string ext = file.GetExtension();

        //     if (ext != Constants.DEFAULT_MAP_EXT)
        //     {
        //         import.Add(file);
        //     }
        // }

        var nonPhxmMaps = Directory.EnumerateFiles($"{Constants.USER_FOLDER}/maps", $"*.*", SearchOption.AllDirectories).Where(f => f.GetExtension().ToLower() == "sspm" || f.GetExtension().ToLower() == "txt");
        await MapParser.BulkImport([.. nonPhxmMaps], notify: true);
        
        // delete after importing
        foreach (string file in nonPhxmMaps)
        {
            File.Delete(file);
        }

        GetViewport().Connect("files_dropped", Callable.From((string[] files) => {
            EmitSignal(SignalName.FilesDropped, files);

            List<string> maps = [];
            List<Replay> replays = [];

            foreach (string file in files)
            {
                string ext = file.GetExtension();

                if (MapParser.IsValidExt(ext))
                {
                    maps.Add(file);
                }
                else
                {
                    switch (ext)
                    {
                        case "phxr":
                            Replay replay = new(file);

                            if (!replay.Valid)
                            {
                                continue;
                            }

                            replays.Add(replay);
                            break;
                    }
                }
            }

            if (maps.Count > 0)
            {
                MapParser.BulkImport([.. maps]);

                if (SceneManager.Scene is MainMenu)
                {
                    var menu = SceneManager.Scene as MainMenu;
                    menu.Transition(menu.PlayMenu);
                }
            }

            if (replays.Count > 0)
            {
                List<Replay> matching = [];

                foreach (Replay replay in replays)
                {
                    if (replay == replays[0])
                    {
                        matching.Add(replay);
                    }
                }

                LegacyRunner.Play(MapParser.Decode(matching[0].MapFilePath), matching[0].Speed, matching[0].StartFrom, matching[0].Modifiers, null, [.. matching]);
            }
        }));

        loaded = true;
    }

    public static void Quit()
    {
        var settings = SettingsManager.Instance.Settings;

        if (Quitting)
        {
            return;
        }

        Quitting = true;

        if (!LegacyRunner.CurrentAttempt.IsReplay)
        {
            LegacyRunner.CurrentAttempt.Stop();
        }

        Stats.TotalPlaytime += (Time.GetTicksUsec() - Constants.STARTED) / 1000000;

        if (loaded)
        {
            SettingsManager.Save();
            Stats.Save();
        }

        Discord.Client.Dispose();
        
        Instance.GetTree().Quit();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (SceneManager.Scene != null && SceneManager.Scene is LegacyRunner)
            {
                Stats.RageQuits++;
            }

            Quit();
        }
    }
}
