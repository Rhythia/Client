using Godot;
using System;
using System.Threading.Tasks;

public partial class FlatPreview : Panel
{
    public Map Map;
    public bool UseSoundManagerStreamPlayer;
    public bool Playing = false;
    public double Time = 0;
    public double Speed = 1;

    private Color bright = new(1, 1, 1, 0.2f);
    private Color transparent = new(1, 1, 1, 0);
    private ColorRect[] tiles = new ColorRect[9];
    private int lastPassedNote = 0;

    public override void _Ready()
    {
        for (int i = 0; i < 9; i++)
        {
            ColorRect tile = new()
            {
                Name = i.ToString(),
                Color = transparent
            };

            AddChild(tile);

            float left = i % 3 / 3f;
            float top = (float)Math.Floor(i / 3f) / 3;
            float third = 1 / 3f;

            tile.SetAnchor(Side.Left, left);
            tile.SetAnchor(Side.Top, top);
            tile.SetAnchor(Side.Right, left + third);
            tile.SetAnchor(Side.Bottom, top + third);

            tiles[i] = tile;
        }
    }

    public override void _Process(double delta)
    {
        float alpha = (float)Math.Min(1, delta * 6);

        foreach (ColorRect tile in tiles)
        {
            tile.Color = tile.Color.Lerp(transparent, alpha);
        }

        double oldTime = Time;

        if (UseSoundManagerStreamPlayer)
        {
            if (SoundManager.Map.Name != Map.Name)
            {
                return;
            }

            Playing = !SoundManager.Song.StreamPaused;
            Time = SoundManager.Song.GetPlaybackPosition() * 1000;
        }
        else if (Playing)
        {
            Time += delta * Speed * 1000;
        }

        if (Time < oldTime)
        {
            Task.Run(() => {
                for (int i = 0; i < Map.Notes.Length; i++)
                {
                    if (Time < Map.Notes[i].Millisecond)
                    {
                        lastPassedNote = i - 1;
                        break;
                    }
                }
            });
        }

        for (int i = Math.Clamp(lastPassedNote + 1, 0, Math.Max(0, Map.Notes.Length - 1)); i < Map.Notes.Length; i++)
        {
            var note = Map.Notes[i];

            if (Time >= note.Millisecond)
            {
                Vector2I pos = new(Math.Clamp((int)Math.Floor(note.X + 1.5), 0, 2), Math.Clamp((int)Math.Floor(-note.Y + 1.5), 0, 2));
                ColorRect tile = tiles[pos.X + 3 * pos.Y];

                tile.Color = bright;
                lastPassedNote = i;
            }
            else
            {
                break;
            }
        }
    }

	public void Setup(Map map, bool useSoundManagerStreamPlayer = false)
	{
        if (Map != null && Map.Name == map.Name) { return; }
        
        Map = map;
        UseSoundManagerStreamPlayer = useSoundManagerStreamPlayer;
        lastPassedNote = 0;
    }

	public void Seek(double seek)
	{
        Time = seek;
    }
}
