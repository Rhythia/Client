using System.IO;
using Godot;

[GlobalClass]
public partial class Constants : Node
{
    public static readonly ulong STARTED = Time.GetTicksUsec();

    public static readonly string ROOT_FOLDER = Directory.GetCurrentDirectory();

    public static readonly string USER_FOLDER = OS.GetUserDataDir();

    public static readonly string DEFAULT_MAP_EXT = "phxm";

    public static readonly bool TEMP_MAP_MODE = false;//OS.GetCmdlineArgs().Length > 0;

    public static readonly double CURSOR_SIZE = 0.2625;

    public static readonly double GRID_SIZE = 3.0;

    public static readonly Vector2 BOUNDS = new((float)(GRID_SIZE / 2 - CURSOR_SIZE / 2), (float)(GRID_SIZE / 2 - CURSOR_SIZE / 2));

    public static readonly double HIT_BOX_SIZE = 0.07;

    public static readonly double HIT_WINDOW = 55;

    public static readonly int BREAK_TIME = 4000;  // used for skipping breaks mid-map

    public static readonly string[] DIFFICULTIES = ["N/A", "Easy", "Medium", "Hard", "Insane", "Illogical"];

    public static readonly Color[] DIFFICULTY_COLORS = [Color.FromHtml("ffffff"), Color.FromHtml("77f379"), Color.FromHtml("fff832"), Color.FromHtml("e24479"), Color.FromHtml("9d6eff"), Color.FromHtml("0094fc")];

    public static readonly Godot.Collections.Dictionary<string, double> MODS_MULTIPLIER_INCREMENT = new()
    {
        ["NoFail"] = 0,
        ["Ghost"] = 0.0675,
        // ["Spin"] = 0.18,
        // ["Flashlight"] = 0.1,
        // ["Chaos"] = 0.07,
        // ["HardRock"] = 0.08
    };
}
