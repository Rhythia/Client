using System.Collections.Generic;
using Godot;

public partial class Lobby : Node
{
    public static Lobby Instance;

    public static List<Player> Players = [];

    public static int PlayersReady = 0;

    /// <summary>
    /// Parsed map reference
    /// </summary>
    public static Map Map;

    public static double Speed = 1;

    /// <summary>
    /// Millisecond timestamp to start the map from
    /// </summary>
    public static double StartFrom = 0;

    public static Dictionary<string, bool> Modifiers = new()
    {
        ["NoFail"] = false,
        ["Ghost"] = false,
        ["Spin"] = false,
        ["Flashlight"] = false,
        ["Chaos"] = false,
        ["HardRock"] = false
    };

    [Signal]
    public delegate void AllReadyEventHandler();

    [Signal]
    public delegate void MapChangedEventHandler(Map map);

    [Signal]
    public delegate void SpeedChangedEventHandler(double speed);

    [Signal]
    public delegate void StartFromChangedEventHandler(double startFrom);

    [Signal]
    public delegate void ModifiersChangedEventHandler(Godot.Collections.Dictionary<string, bool> mods);

    public override void _Ready()
    {
        Instance = this;

        MapManager.Selected.ValueChanged += (_, _) =>
        {
            var map = MapManager.Selected.Value;

            if (Map == null || Map.Name != map.Name)
            {
                SetMap(map);
            }
        };
    }

    public static void Enter()
    {
        AddPlayer(new("You"));
    }

    public static void Leave()
    {
        Players = [];
    }

    public static void PlayerReady(string name, bool ready = true)
    {
        // if (Players.ContainsKey(name))
        // {
        //     if (Players[name].Ready == ready)
        //     {
        //         return;
        //     }

        //     Players[name].Ready = ready;
        // }

        PlayersReady += ready ? 1 : -1;

        if (PlayersReady == Players.Count)
        {
            Instance.EmitSignal(SignalName.AllReady);
        }
    }

    public static void PlayerUnready(string name)
    {
        PlayerReady(name, false);
    }

    public static void AddPlayer(Player player)
    {
        Players.Add(player);
    }

    public static void RemovePlayer(Player player)
    {
        Players.Remove(player);
    }

    public static void SetMap(Map map)
    {
        Map = map;

        Instance.EmitSignal(SignalName.MapChanged, Map);
    }

    public static void SetSpeed(double speed)
    {
        Speed = speed;

        Instance.EmitSignal(SignalName.SpeedChanged, Speed);
    }

    public static void SetStartFrom(double startFrom)
    {
        StartFrom = startFrom;

        Instance.EmitSignal(SignalName.StartFromChanged, StartFrom);
    }

    public static void SetModifier(string mod, bool active)
    {
        if (!Modifiers.ContainsKey(mod)) { return; }

        Modifiers[mod] = active;

        Godot.Collections.Dictionary<string, bool> mods = new(Modifiers);

        Instance.EmitSignal(SignalName.ModifiersChanged, mods);
    }
}
