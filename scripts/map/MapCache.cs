using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Octokit;
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
        if (Rhythia.TempMode)
        {
            return;
        }

        try
        {
            // string[] files = Directory.GetFiles(MapUtil.MapsFolder, $"*.{Constants.DEFAULT_MAP_EXT}", SearchOption.AllDirectories);

            // List<string> mapsList = Directory
            //     .GetFiles(MapUtil.MapsFolder, $"*.{Constants.DEFAULT_MAP_EXT}", SearchOption.AllDirectories)
            //     .Concat(Directory.GetDirectories(MapUtil.MapsFolder, "*", SearchOption.AllDirectories))
            //     .ToList();

            // Map files go first since they will be encoded to folders after they get parsed in MapParser.cs
            List<string> mapsList = Directory.GetDirectories(MapUtil.MapsFolder, "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(MapUtil.MapsFolder,$"*.{Constants.DEFAULT_MAP_EXT}", SearchOption.AllDirectories))
                .ToList();

            string[] toParseMaps = mapsList.ToArray();

            if (fullSync)
            {
                syncFiles(toParseMaps);
                addNonCachedFiles(toParseMaps);

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

    private static void syncFiles(string[] toParseMaps)
    {
        var maps = FetchAll();

        FilesToSync.Value = maps.Count;
        FilesSynced.Value = 0;

        for (int i = 0; i < toParseMaps.Length; i++)
        {
            toParseMaps[i] = BackSlashToForwardSlash(toParseMaps[i]);
        }

        var mapsHashSet = toParseMaps.ToHashSet();

        foreach (var map in maps)
        {
            string mapPath = BackSlashToForwardSlash(map.FolderPath);

            // Checks if the map is actually inside the maps folder
            if (mapsHashSet.Contains(mapPath))
            {
                string checksum = GetMd5Checksum(mapPath);
                DateTime metadataModifiedDate = File.GetLastWriteTime(Path.Combine(mapPath, "metadata.json"));
                DateTime objectModifiedDate = File.GetLastWriteTime(Path.Combine(mapPath, "objects.phxmo"));

                GD.Print($"{Path.Combine(mapPath, "metadata.json")} = {metadataModifiedDate}");

                bool metadataCheck = metadataModifiedDate == map.LastModifiedMetadata;
                bool objectsCheck = objectModifiedDate == map.LastModifiedNotes;

                if (map.MetadataObjectHash == checksum || metadataCheck || objectsCheck)
                {
                    GD.Print("skip");
                    FilesSynced.Value += 1;
                    continue;
                }

                Map newMap;

                try
                {
                    newMap = MapParser.Decode(mapPath, null, false, true);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    if (File.Exists(mapPath))
                    {
                        File.Delete(mapPath);
                    }
                    else if (Directory.Exists(mapPath))
                    {
                        Directory.Delete(mapPath, true);
                    }
                    DatabaseService.Connection.Delete(map);

                    FilesSynced.Value += 1;
                    continue;
                }

                newMap.LastModifiedMetadata = metadataModifiedDate;
                newMap.LastModifiedNotes = objectModifiedDate;

                newMap.Id = map.Id;
                newMap.MetadataObjectHash = checksum;

                DatabaseService.Connection.Update(newMap);
                // InsertIntoMapCacheFolder(map);
                Logger.Log($"Updated cached map: {newMap.Name}");
                FilesSynced.Value += 1;
                continue;
            }
            else
            {
                // removeCacheFolder(map);
                try
                {
                    Directory.Delete($"{MapUtil.MapsFolder}/{map.Name}", true);
                }
                catch
                {
                    return;
                }
                DatabaseService.Connection.Delete(map);
                Logger.Log($"Removed {mapPath} from the cache, as it no longer exists.");

                FilesSynced.Value += 1;
            }
        }
    }

    // public static void InsertIntoMapCacheFolder(Map map)
    // {
    //     string path = $"{MapUtil.MapsCacheFolder}/{map.Name}";

    //     // for phxm shit i guess
    //     if (File.Exists(map.FolderPath))
    //     {
    //         using var stream = File.OpenRead(map.FolderPath);
    //         var archive = new ZipArchive(stream);

    //         if (Directory.Exists(path))
    //         {
    //             Directory.Delete(path, true);
    //         }

    //         archive.ExtractToDirectory(path, true);
    //     }
    //     else if (Directory.Exists(map.FolderPath)) 
    //     {
    //         GD.Print("PATH PATH PATH!!!");
    //         if (Directory.Exists(path))
    //         {
    //             Directory.Delete(path, true);
    //         }

    //         Directory.CreateDirectory(path);

    //         foreach (string file in Directory.GetFiles(map.FolderPath))
    //         {
    //             string destFile = Path.Combine(path, Path.GetFileName(file));
    //             File.Copy(file, destFile, overwrite: true);
    //         }
    //     }
        
    //     // using var stream = File.OpenRead(map.FilePath);
    //     // var archive = new ZipArchive(stream);

    //     // if (Directory.Exists(path))
    //     // {
    //     //     Directory.Delete(path, true);
    //     // }

    //     // archive.ExtractToDirectory(path, true);

    // }

    // private static void removeCacheFolder(Map map)
    // {
    //     try
    //     {
    //         Directory.Delete($"{MapUtil.MapsCacheFolder}/{map.Name}", true);
    //     }
    //     catch
    //     {
    //         return;
    //     }
    // }

    private static void addNonCachedFiles(string[] toParseMaps)
    {
        var maps = FetchAll();

        HashSet<string> hashSet = new();
        maps.ForEach(map => hashSet.Add(map.FolderPath));

        foreach (string toParseMap in toParseMaps)
        {
            if (hashSet.Contains(BackSlashToForwardSlash(toParseMap)))
            {
                continue;
            }

            FilesToSync.Value += 1;

            try
            {
                var map = MapParser.Decode(toParseMap);
                // var map = !Directory.Exists(file) ? MapParser.Decode(file) : MapParser.DecodeFolder(file); 
                map.FolderPath = $"{Constants.USER_FOLDER}/maps/{map.Name}";
                // map.FilePath = !Directory.Exists(map.FilePath) ? $"{Constants.USER_FOLDER}/maps/{map.Name}.{Constants.DEFAULT_MAP_EXT}" : $"{Constants.USER_FOLDER}/maps/{map.Name}";
                map.MetadataObjectHash = GetMd5Checksum(toParseMap);
                InsertMap(map);
            }
            catch
            {
                Directory.Delete(toParseMap, true);
                Logger.Log($"Failed to add map non-cached map");
            }

            FilesSynced.Value += 1;
        }
    }

    public static int InsertMap(Map map)
    {
        var existing = DatabaseService.Connection.Find<Map>(x => x.MetadataObjectHash == map.MetadataObjectHash);
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

            return DatabaseService.Connection.Get<Map>(x => x.MetadataObjectHash == map.MetadataObjectHash).Id;
        }
        catch (Exception e)
        {
            if (existing == null || updated == null)
            {
                Logger.Error(e.Message);
                return -1;
            }

            string newPath = Path.Combine(MapUtil.MapsFolder, map.Name);
            string existingPath = Path.Combine(MapUtil.MapsFolder, existing?.FolderPath ?? updated.FolderPath);

            if (existingPath != newPath)
            {
                Directory.Delete(newPath, true);
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
                string path = $"{MapUtil.MapsFolder}/{map.Name}";

                if (map.Cover == Map.DefaultCover && File.Exists($"{path}/cover.png"))
                {
                    byte[] coverBuffer = File.ReadAllBytes($"{path}/cover.png");
                    if (coverBuffer == null || coverBuffer.Length == 0)
                    {
                        continue;
                    }

                    Image image = Misc.LoadImageFromBuffer(coverBuffer);

                    if (image != null)
                    {
                        Callable.From(() =>
                        {
                            if (MapManager.Maps.Contains(map))
                            {
                                map.Cover = ImageTexture.CreateFromImage(image);
                            }
                        }).CallDeferred();
                    }

                }
            }
        });

        if (maps.Count < 1)
        {
            MapManager.Maps = [];
            return;
        }

        var sortedMaps = maps.Where(x => x.Favorite).OrderBy(x => x.PrettyTitle).ToList();

        sortedMaps.AddRange(maps.Where(x => !x.Favorite).OrderBy(x => x.PrettyTitle));

        foreach (var map in sortedMaps)
        {
            MapManager.Sanitize(map);
        }

        MapManager.Maps = sortedMaps;
    }

    // public static List<MapSet> ConvertToMapSets(IEnumerable<Map> maps)
    // {
    //     var groupedMaps = maps
    //         .GroupBy(u => u.Collection)
    //         .Select(x => x.ToList())
    //         .ToList();

    //     var mapSets = new List<MapSet>();

    //     foreach (var mapSet in groupedMaps)
    //     {
    //         var set = new MapSet()
    //         {
    //             Directory = mapSet.First().Collection,
    //             Maps = mapSet
    //         };

    //         set.Maps.ForEach(x => x.MapSet = set);
    //         mapSets.Add(set);
    //     }

    //     return mapSets;
    // }

    public static string GetMd5Checksum(string path)
    {
        string metadataPath = Path.Combine(path, "metadata.json");
        string objectsPath = Path.Combine(path, "objects.phxmo");
        byte[] hash = Misc.HashFiles([metadataPath, objectsPath]);

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

        // using (var md5 = MD5.Create())
        // {
        //     using (var stream = File.OpenRead(path))
        //     {
        //         return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
        //     }
        // }
    }

    public static List<Map> FetchAll() => DatabaseService.Connection.Table<Map>().ToList();

    public static string BackSlashToForwardSlash(string path) => path.Replace("\\", "/");
}
