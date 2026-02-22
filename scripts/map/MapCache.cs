using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Util;

public static class MapCache
{
    public static Bindable<int> FilesToSync = new(0);
    public static Bindable<int> FilesSynced = new(0);
    public static event Action<int> OnFilesSyncFinished;

    public static void Initialize()
    {
        DatabaseService.Connection.CreateTable<Map>();
    }

    public static void Load(bool fullSync)
    {
        try
        {
            string[] files = Directory.GetFiles(MapUtil.MapsFolder, $"*.{Constants.DEFAULT_MAP_EXT}", SearchOption.AllDirectories);

            if (fullSync)
            {
                syncFiles(files);
                addNonCachedFiles(files);

                OnFilesSyncFinished?.Invoke(FilesSynced.Value);
                FilesToSync.Value = 0;
                FilesSynced.Value = 0;
            }

            OrderAndSetMaps();
        }
        catch
        {
            OrderAndSetMaps();
        }
    }

    private static void syncFiles(string[] files)
    {
        var maps = FetchAll();

        FilesToSync.Value = maps.Count;
        FilesSynced.Value = 0;

        for (int i = 0; i < files.Length; i++)
        {
            files[i] = BackSlashToForwardSlash(files[i]);
        }

        var filesHashSet = files.ToHashSet();

        foreach (var map in maps)
        {
            string filePath = BackSlashToForwardSlash(map.FilePath);
            
            if (filesHashSet.Contains(filePath))
            {
                string checksum = GetMd5Checksum(filePath);

                if (map.Hash == checksum)
                {
                    if (!Directory.Exists($"{MapUtil.MapsCacheFolder}/{map.Name}"))
                    {
                        InsertIntoMapCacheFolder(map);
                    }
                    
                    FilesSynced.Value += 1;
                    continue;
                }

                Map newMap;

                try
                {
                    newMap = MapParser.Decode(filePath, null, false, true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    File.Delete(filePath);
                    DatabaseService.Connection.Delete(map);

                    FilesSynced.Value += 1;
                    continue;
                }

                newMap.Id = map.Id;
                newMap.Hash = checksum;

                DatabaseService.Connection.Update(newMap);
                InsertIntoMapCacheFolder(map);
                Logger.Log($"Updated cached map: {newMap.Name}");

                FilesSynced.Value += 1;
                continue;
            }
            else
            {
                removeCacheFolder(map);
                DatabaseService.Connection.Delete(map);
                Logger.Log($"Removed {filePath} from the cache, as it no longer exists.");

                FilesSynced.Value += 1;
            }
        }
    }

    public static void InsertIntoMapCacheFolder(Map map)
    {
        string path = $"{MapUtil.MapsCacheFolder}/{map.Name}";
        using var stream = File.OpenRead(map.FilePath);
        var archive = new ZipArchive(stream);

        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        archive.ExtractToDirectory(path, true);
    }

    private static void removeCacheFolder(Map map)
    {
        try
        {
            Directory.Delete($"{MapUtil.MapsCacheFolder}/{map.Name}", true);
        }
        catch
        {
            return;
        }
    }

    private static void addNonCachedFiles(string[] files)
    {
        var maps = FetchAll();

        HashSet<string> hashSet = new();
        maps.ForEach(map => hashSet.Add(map.FilePath));

        foreach (string file in files)
        {
            if (hashSet.Contains(BackSlashToForwardSlash(file)))
            {
                continue;
            }

            FilesToSync.Value += 1;

            try
            {
                var map = MapParser.Decode(file);
                map.Collection = file.GetBaseDir().Split("/")[^1];
                map.FilePath = $"{Constants.USER_FOLDER}/maps/{map.Collection}/{map.Name}.{Constants.DEFAULT_MAP_EXT}";
                map.Hash = GetMd5Checksum(file);
                File.Move(file, map.FilePath);
                InsertMap(map);
            }
            catch
            {
                File.Delete(file);
                Logger.Log($"Failed to add map non-cached map");
            }

            FilesSynced.Value += 1;
        }
    }

    public static int InsertMap(Map map)
    {
        var existing = DatabaseService.Connection.Find<Map>(x => x.Hash == x.Hash);
        var updated = DatabaseService.Connection.Find<Map>(x => x.Name == map.Name);
        try
        {
            if (updated != null && existing != null)
            {
                map.Id = updated.Id;
                UpdateMap(map);
                return map.Id;
            }

            DatabaseService.Connection.Insert(map);
            InsertIntoMapCacheFolder(map);

            return DatabaseService.Connection.Get<Map>(x => x.Hash == map.Hash).Id;
        }
        catch (Exception e)
        {
            if (existing == null || updated == null)
            {
                Logger.Error(e.Message);
                return -1;
            }

            string newPath = Path.Combine(MapUtil.MapsFolder, map.Collection, map.Name);
            string existingPath = Path.Combine(MapUtil.MapsFolder, map.Collection, existing?.FilePath ?? updated.FilePath);

            if (existingPath != newPath)
            {
                File.Delete(newPath);
                return -1;
            }

            return -1;
        }
    }

    public static void UpdateMap(Map map)
    {
        try
        {
            DatabaseService.Connection.Update(map);
            InsertIntoMapCacheFolder(map);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
        }
    }

    public static void RemoveMap(Map map)
    {
        try
        {
            DatabaseService.Connection.Delete(map);
        }
        catch (Exception e)
        {
            Logger.Error(e.Message);
        }
    }



    public static void OrderAndSetMaps()
    {
        var maps = FetchAll();

        //TODO: not make this terrible
        Task.Run(() =>
        {
            foreach (var map in maps)
            {
                string path = $"{MapUtil.MapsCacheFolder}/{map.Name}";

                if (map.Cover == Map.DefaultCover && File.Exists($"{path}/cover.png"))
                {
                    byte[] coverBuffer = File.ReadAllBytes($"{path}/cover.png");
                    Image image = Util.Misc.LoadImageFromBuffer(coverBuffer);

                    if (image != null){
                        Callable.From(() => {
                            map.Cover = ImageTexture.CreateFromImage(image);
                            }).CallDeferred();
                    }
                    
                }
            }
        });

        if (maps.Count < 1)
        {
            MapManager.Maps = new();
            return;
        }

        var sortedMaps = maps.Where(x => x.Favorite).OrderBy(x => x.PrettyTitle).ToList();

        sortedMaps.AddRange(maps.Where(x => !x.Favorite).OrderBy(x => x.PrettyTitle));

        MapManager.Maps = sortedMaps;
    }

    public static List<MapSet> ConvertToMapSets(IEnumerable<Map> maps)
    {
        var groupedMaps = maps
            .GroupBy(u => u.Collection)
            .Select(x => x.ToList())
            .ToList();

        var mapSets = new List<MapSet>();

        foreach (var mapSet in groupedMaps)
        {
            var set = new MapSet()
            {
                Directory = mapSet.First().Collection,
                Maps = mapSet
            };

            set.Maps.ForEach(x => x.MapSet = set);
            mapSets.Add(set);
        }

        return mapSets;
    }

    public static string GetMd5Checksum(string path)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
            }
        }
    }

    public static List<Map> FetchAll() => DatabaseService.Connection.Table<Map>().ToList();

    public static string BackSlashToForwardSlash(string path) => path.Replace("\\", "/");
}
