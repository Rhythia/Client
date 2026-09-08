using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class Rhythia : Node
{
    private static bool loaded = false;

    [Signal]
    public delegate void FilesDroppedEventHandler(string[] files);

    public static Rhythia Instance;
    public static bool Quitting { get; private set; } = false;

    // Updates once on startup
    public static CameraMode[] CameraModes = [];
    public static Modifier[] Modifiers = [];

    // For Temporary Maps
    public static List<Modifier> TempMods = [new NoFailModifier()];
    public static CameraMode TempCam = new CameraLock();

    public static bool TempMode = false;
    public static string TextFilePath = null;
    public static string AudioFilePath = null;
    public static string StartFromParameter = "";
    public static string SpeedParameter = "";

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
        Stats.Initialize();
        Stats.Instance.GamesOpened++;

        // Map import
        var nonConvertedMaps = Directory.EnumerateFiles($"{Constants.USER_FOLDER}/maps", $"*.*", SearchOption.AllDirectories).Where(f =>
            f.GetExtension().ToLower() != Constants.DEFAULT_MAP_EXT
            && MapParser.IsValidExt(f.GetExtension().ToLower())
        );

        await MapParser.BulkImport([.. nonConvertedMaps], notify: true);

        foreach (string file in nonConvertedMaps)
        {
            File.Delete(file);
        }

        // Temporary map testing support
        string[] cmdArgs = OS.GetCmdlineArgs();

        foreach (string command in cmdArgs)
        {
            string[] split = command.Split("=");

            switch (split[0])
            {
                case "--t":
                    TextFilePath = split[1];
                    break;
                case "--a":
                    AudioFilePath = split[1];
                    break;
                case "--sp":
                    SpeedParameter = split[1];
                    break;
                case "--sf":
                    StartFromParameter = split[1];
                    break;
                default:
                    break;
            }
        }

        TempMode = TextFilePath != null;

        if (TempMode)
        {
            var tempMap = MapParser.Decode(TextFilePath, AudioFilePath);

            Game.Play(tempMap, 1.0, 0.0, TempCam, TempMods);
        }

        RegisterCameraModes();
        RegisterModifiers();

        GetViewport().Connect("files_dropped", Callable.From((string[] files) =>
        {
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

                Game.Play(MapParser.Decode(matching[0].MapFilePath), matching[0].Speed, matching[0].StartFrom, matching[0].CameraMode, matching[0].Modifiers, null, [.. matching]);
            }
        }));

        loaded = true;
    }

    public static void RegisterCameraModes()
    {
        CameraModes = [
            new CameraLock(),
            new CameraSpin()
        ];
    }

    public static void RegisterModifiers()
    {
        Modifiers = [
            new NoFailModifier(),
            new GhostModifier(),
            new StrobeModifier(),
            new ChaosModifier(),
            new VortexModifier(),
            new EarthquakeModifier(),
            new HorizontalFlipModifier(),
            new VerticalFlipModifier()
        ];
    }

    public static void Quit()
    {
        if (Quitting)
        {
            return;
        }

        Quitting = true;

        Logger.Log("Attempting to quit...");

        bool playing = (Game.Instance?.Runner?.Playing ?? false) && (!Game.Attempt?.IsReplay ?? false);

        if (playing)
        {
            Game.Instance.Runner.Stop(false);
        }

        Stats.Instance.TotalPlaytime += (Time.GetTicksUsec() - Constants.STARTED) / 1000000;

        if (loaded)
        {
            SettingsManager.Save();
            Stats.Instance.Save();
        }

        Discord.Client.Dispose();

        var quitTween = Instance.CreateTween();
        quitTween.TweenCallback(Callable.From(() =>
        {
            Logger.Log("Quitting");
            Instance.GetTree().Quit();
        })).SetDelay(0.5);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (SceneManager.Scene != null && SceneManager.Scene is Game)
            {
                Stats.Instance.RageQuits++;
            }

            Quit();
        }
        else if (what == NotificationApplicationFocusOut)
        {
            Engine.MaxFps = 30;
        }
        else if (what == NotificationApplicationFocusIn)
        {
            var settings = SettingsManager.Instance.Settings;
            Engine.MaxFps = settings.LockFPS ? settings.FPS : 0;
        }
    }
}
