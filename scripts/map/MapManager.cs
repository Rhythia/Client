
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;

public partial class MapManager : Node
{
    public static Bindable<Map> Selected { get; set; } = new(null);

    public static List<Map> Maps { get; set; } = new();

    public static event Action<Map> MapDeleted;

    public static event Action<Map> MapUpdated;

    public static bool Initialized = false;

    public static event Action<List<Map>> MapsInitialized;

    public override void _Ready()
    {
        MapCache.Initialize();
        Task.Run(() =>
        {
            MapCache.Load(true);

            if (!Initialized)
            {
                Initialized = true;
                MapsInitialized?.Invoke(Maps);
            }
        });
    }

    public static void Select(Map map)
    {
        Selected.Value = GetMapById(map.Id);
    }

    public static Map GetMapById(int id)
    {
        return Maps.Where(x => x.Id == id).First();
    }

    public static void Update(Map map)
    {
        MapCache.UpdateMap(map);
        MapUpdated?.Invoke(map);
    }

    public static void InsertVideo(Map map, string path)
    {
        Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        byte[] videoBuffer = file.GetBuffer((long)file.GetLength());
        file.Close();

        map.VideoBuffer = videoBuffer;

        var oldmap = MapParser.Decode(map.FilePath);

        map.Mappers = map.CachedMappers.Split("_");
        map.AudioBuffer = oldmap.AudioBuffer;
        map.CoverBuffer = oldmap.CoverBuffer;

        File.Delete(map.FilePath);

        MapParser.Encode(map);
        map.Hash = MapCache.GetMd5Checksum(map.FilePath);
        Update(map);
    }

    public static void RemoveVideo(Map map)
    {
        _ = Godot.FileAccess.Open(map.FilePath, Godot.FileAccess.ModeFlags.Read);

        map.VideoBuffer = null;

        var oldmap = MapParser.Decode(map.FilePath);
        map.Mappers = map.PrettyMappers.Split(" ");
        map.AudioBuffer = oldmap.AudioBuffer;
        map.CoverBuffer = oldmap.CoverBuffer;

        File.Delete(map.FilePath);

        MapParser.Encode(map);
        map.Hash = MapCache.GetMd5Checksum(map.FilePath);
        Update(map);
    }

    public static void Delete(Map map)
    {
        try
        {
            try
            {
                File.Delete(map.FilePath);
            }
            catch
            {
                if (File.Exists(map.FilePath))
                {
                    Logger.Error("Unable to delete map");
                    return;
                }
            }

            MapCache.RemoveMap(map);
            Maps.RemoveAll(x => x.Id == map.Id);


            Callable.From(() =>
            {
                _ = ToastNotification.Notify($"Deleted {map.PrettyTitle}!");
            }).CallDeferred();
            Callable.From(() => MapDeleted?.Invoke(map)).CallDeferred();
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
        }
    }

    public static string MapsFolder => $"{Constants.USER_FOLDER}/maps";
}
