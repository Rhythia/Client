using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using SQLite;

public partial class Map : RefCounted
{

    public static Texture2D DefaultCover = GD.Load<Texture2D>("res://textures/empty.png");

    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Hash { get; set; }

    public string Collection { get; set; } = string.Empty;

    [Ignore]
    public MapSet MapSet { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public bool Favorite { get; set; }

    [Ignore]
    public bool Ephemeral { get; set; } = false;

    public string Artist { get; set; } = string.Empty;

    public string ArtistLink { get; set; } = string.Empty;

    public string ArtistPlatform { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string PrettyTitle { get; set; } = string.Empty;

    public float Rating { get; set; } = 0;

    public string CachedMappers { get; set; } = string.Empty;

    [Ignore]
    public string[] Mappers { get; set; } = [];

    public string PrettyMappers { get; set; } = string.Empty;

    public string DifficultyName { get; set; } = string.Empty;

    public int Difficulty { get; set; } = 0;

    public int Length { get; set; } = 0;

    [Ignore]
    public byte[] AudioBuffer { get; set; } = [];

    public string AudioExt { get; set; } = string.Empty;

    private Texture2D cover = DefaultCover;

    [Ignore]
    public Texture2D Cover { get => getCover(); set => cover = value; }

    [Ignore]
    public AudioStream Audio { get; set; } = null;

    [Ignore]
    public byte[] CoverBuffer { get; set; } = [];

    [Ignore]
    public byte[] VideoBuffer { get; set; } = [];

    private Note[] notes;

    [Ignore]
    public Note[] Notes { get => notes ?? TryParseNotes(); set => notes = value; }

    public Note[] TryParseNotes()
    {
        try
        {
            notes = MapParser.DecodePHXMO($"{MapUtil.MapsCacheFolder}/{Name}/objects.phxmo");
            return notes;
        }
        catch
        {
            return [];
        }
    }
    
    private Texture2D getCover()
    {
        string path = $"{MapUtil.MapsCacheFolder}/{Name}";

        if (cover == DefaultCover && File.Exists($"{path}/cover.png"))
    {
        byte[] coverBuffer = File.ReadAllBytes($"{path}/cover.png");
        Image image = Util.Misc.LoadImageFromBuffer(coverBuffer);
        if (image != null)
            cover = ImageTexture.CreateFromImage(image);
        }

    return cover;
}
    public Map() { }

    public Map(string filePath, Note[] data = null, string id = null, string artist = "", string title = "", float rating = 0, string[] mappers = null, int difficulty = 0, string difficultyName = null, int? length = null, byte[] audioBuffer = null, byte[] coverBuffer = null, byte[] videoBuffer = null, bool ephemeral = false, string artistLink = "", string artistPlatform = "")
    {
        FilePath = filePath;
        Ephemeral = ephemeral;
        Artist = (artist ?? "").StripEscapes();
        ArtistLink = artistLink;
        ArtistPlatform = artistPlatform;
        Title = (title ?? "").StripEscapes();
        PrettyTitle = Artist != "" ? $"{Artist} - {Title}" : Title;
        Rating = rating;
        Mappers = mappers ?? ["N/A"];
        CachedMappers = mappers.Join("_");
        PrettyMappers = "";
        Difficulty = difficulty;
        DifficultyName = difficultyName?.StripEscapes() ?? Constants.DIFFICULTIES[Difficulty];
        AudioBuffer = audioBuffer;
        CoverBuffer = coverBuffer;
        VideoBuffer = videoBuffer;
        Notes = data ?? Array.Empty<Note>();
        Length = length ?? Notes[^1].Millisecond;
        Name = (id.Replace(" ", "_") ?? new Regex("[^a-zA-Z0-9_-]").Replace($"{Mappers.Stringify()}_{PrettyTitle}".Replace(" ", "_"), ""));
        AudioExt = (AudioBuffer != null && Encoding.UTF8.GetString(AudioBuffer[0..4]) == "OggS") ? "ogg" : "mp3";

        foreach (string mapper in Mappers)
        {
            PrettyMappers += $"{mapper}, ";
        }

        PrettyMappers = PrettyMappers.Substr(0, PrettyMappers.Length - 2).StripEscapes();
    }

    public string EncodeMeta()
    {
        return Json.Stringify(new Godot.Collections.Dictionary()
        {
            ["ID"] = Name,
            ["Artist"] = Artist,
            ["ArtistLink"] = ArtistLink,
            ["ArtistPlatform"] = ArtistPlatform,
            ["Title"] = Title,
            ["Rating"] = Rating,
            ["Mappers"] = Mappers,
            ["Difficulty"] = Difficulty,
            ["DifficultyName"] = DifficultyName,
            ["Length"] = Length,
            ["HasAudio"] = AudioBuffer != null,
            ["HasCover"] = CoverBuffer != null,
            ["HasVideo"] = VideoBuffer != null,
            ["AudioExt"] = AudioExt
        }, "\t");
    }
}
