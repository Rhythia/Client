using System.IO;
using Godot;

[GlobalClass]
public partial class Constants : Node
{
    public static readonly ulong STARTED = Time.GetTicksUsec();

    public static readonly string ROOT_FOLDER = Directory.GetCurrentDirectory();

    public static readonly string USER_FOLDER = OS.GetUserDataDir();

    public static readonly string NIGHTLY_FOLDER = $"{Path.GetDirectoryName(USER_FOLDER)}/SoundSpacePlus";

    public static readonly string DEFAULT_MAP_EXT = "phxm";

    public static readonly bool TEMP_MAP_MODE = false;//OS.GetCmdlineArgs().Length > 0;

    public static readonly double CURSOR_SIZE = 0.2625;

    public static readonly double GRID_SIZE = 3.0;

    public static readonly Vector2 BOUNDS = new((float)(GRID_SIZE / 2 - CURSOR_SIZE / 2), (float)(GRID_SIZE / 2 - CURSOR_SIZE / 2));

    public static readonly double HIT_BOX_SIZE = 0.07;

    public static readonly double HIT_WINDOW = 55;

    public static readonly int BREAK_TIME = 4000;  // used for skipping breaks mid-map

    public static readonly string[] DIFFICULTIES = ["N/A", "Easy", "Medium", "Hard", "Insane", "Illogical"];

    public static readonly Color[] DIFFICULTY_COLORS = [new(0xffffffff), new(0x77f379ff), new(0xfff832ff), new(0xe24479ff), new(0x9d6effff), new(0x0094fcff)];
}
