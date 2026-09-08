using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Util;

public static class MapCache
{
    public static Bindable<int> FilesToSync = new(0);
    public static Bindable<int> FilesSynced = new(0);
    public static event Action<int> OnFilesSyncFinished;
    public static bool OldCacheFormat = false;
    public static List<string> MapsToBeFavorited = new();

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
            // Map files (.phxm, .sspm, etc) go first since they will be encoded to folders after they get parsed in MapParser.cs -fog
            List<string> mapsList = Directory.GetFiles(MapUtil.MapsFolder, $"*.{Constants.DEFAULT_MAP_EXT}", SearchOption.AllDirectories)
                    .Concat(Directory.GetDirectories(MapUtil.MapsFolder, "*", SearchOption.AllDirectories))
                    .ToList();

            string[] toParseMaps = mapsList.ToArray();

            if (fullSync)
            {
                syncFiles(toParseMaps);
                addNonCachedFiles(toParseMaps);

                // Old caching format used to just extract .phxm files, essentially doubling your game size
                if (OldCacheFormat && Directory.Exists($"{Constants.USER_FOLDER}/cache/maps"))
                {
                    Directory.Delete($"{Constants.USER_FOLDER}/cache/maps", true);
                }

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

        if (maps.Count > 0)
        {
            if (maps[0].CacheVersion is null || maps[0].CacheVersion <= 1)
            {
                OldCacheFormat = true;
                Logger.Log("Old map cache detected! Re-converting...");
            }
        }

        FilesToSync.Value = maps.Count;
        FilesSynced.Value = 0;

        for (int i = 0; i < toParseMaps.Length; i++)
        {
            toParseMaps[i] = BackSlashToForwardSlash(toParseMaps[i]);
        }

        var mapsHashSet = toParseMaps.ToHashSet();

        // Debug Variables that will be printed in case of problems
        int deletedMapInt = 0;
        int convertedMaps = 0;

        foreach (var map in maps)
        {
            string mapPath = BackSlashToForwardSlash(map.FolderPath);

            if (OldCacheFormat && map.Favorite)
            {
                // The new cache re-writes it from scratch with a new format, so we will need to store these for later
                MapsToBeFavorited.Add(map.Name);
            }

            if (mapsHashSet.Contains(mapPath))
            {
                DateTime metadataModifiedDate = File.GetLastWriteTime(Path.Combine(mapPath, "metadata.json"));
                DateTime objectModifiedDate = File.GetLastWriteTime(Path.Combine(mapPath, "objects.phxmo"));

                // Time must be converted to string because the SQLite library doesn't support DateTime types -fog
                string metadataResult = metadataModifiedDate.ToString();
                string notesResult = objectModifiedDate.ToString();

                bool metadataCheck = map.LastModifiedMetadata == metadataResult;
                bool objectsCheck = map.LastModifiedNotes == notesResult;

                // Last modified comes before checksum since it is faster -fog
                if (metadataCheck || objectsCheck)
                {
                    FilesSynced.Value++;
                    continue;
                }

                string checksum = GetMd5Checksum(mapPath);
                if (map.MetadataObjectHash == checksum)
                {
                    FilesSynced.Value++;
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

                    FilesSynced.Value++;
                    continue;
                }

                newMap.LastModifiedMetadata = metadataModifiedDate.ToString();
                newMap.LastModifiedNotes = objectModifiedDate.ToString();

                newMap.Id = map.Id;
                newMap.MetadataObjectHash = checksum;

                DatabaseService.Connection.Update(newMap);
                Logger.Log($"Updated cached map: {newMap.Name}");
                FilesSynced.Value++;
                continue;
            }
            else
            {
                if (Directory.Exists($"{MapUtil.MapsFolder}/{map.Name}"))
                {
                    // Check if valid map
                    if (!File.Exists($"{MapUtil.MapsFolder}/{map.Name}/metadata.json") || !File.Exists($"{MapUtil.MapsFolder}/{map.Name}/objects.phxmo"))
                    {
                        Directory.Delete($"{MapUtil.MapsFolder}/{map.Name}", true);
                    }
                }

                DatabaseService.Connection.Delete(map);
                deletedMapInt++;
                // Logger.Log($"Removed {mapPath} from the cache, as it no longer exists.");

                FilesSynced.Value++;
            }
        }

        if (deletedMapInt > 0)
        {
            Logger.Log($"Removed {deletedMapInt} maps from the cache.");
        }
        if (convertedMaps > 0)
        {
            Logger.Log($"Converted {convertedMaps} maps from the old cache.");
        }
    }

    private static void addNonCachedFiles(string[] toParseMaps)
    {
        var maps = FetchAll();

        HashSet<string> hashSet = new();
        maps.ForEach(map => hashSet.Add(map.FolderPath));

        // Maps that need to be parsed - maps in database cache
        FilesToSync.Value = toParseMaps.Count() - maps.Count();
        FilesSynced.Value = 0;

        // For Old Cache version
        if (toParseMaps.Contains($"{Constants.USER_FOLDER}/maps/default"))
        {
            FilesToSync.Value--;
        }

        Logger.Log($"Decoding {toParseMaps.Length} maps\nFiles To Sync: {FilesToSync.Value}");

        foreach (string toParseMap in toParseMaps)
        {
            // If the map path already exists in the map cache, skip it
            if (hashSet.Contains(BackSlashToForwardSlash(toParseMap)))
            {
                continue;
            }

            try
            {
                var map = MapParser.Decode(toParseMap);
                if (map is null)
                {
                    // Directory.Delete(toParseMap, true);
                    Logger.Log($"Failed to add map non-cached map {toParseMap}");
                    continue;
                }

                map.FolderPath = $"{Constants.USER_FOLDER}/maps/{map.Name}";
                map.MetadataObjectHash = GetMd5Checksum(map.FolderPath);

                if (OldCacheFormat)
                {
                    if (MapsToBeFavorited.Contains(map.Name))
                    {
                        map.Favorite = true;
                    }
                }

                InsertMap(map);
            }
            catch (Exception exception)
            {
                // Directory.Delete(toParseMap, true);
                Logger.Log($"Failed to add map non-cached map {toParseMap}");
                Logger.Error(exception);
            }

            FilesSynced.Value++;
        }

        Logger.Log($"Files Synced: {FilesSynced.Value}");
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

    public static string GetMd5Checksum(string path)
    {
        string metadataPath = Path.Combine(path, "metadata.json");
        string objectsPath = Path.Combine(path, "objects.phxmo");
        byte[] hash = Misc.HashFiles([metadataPath, objectsPath]);

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
    }

    public static List<Map> FetchAll() => DatabaseService.Connection.Table<Map>().ToList();

    public static string BackSlashToForwardSlash(string path) => path.Replace("\\", "/");
}
