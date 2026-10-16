using System.Collections.Generic;
using System.IO;
using System.Linq;

internal static class MapUtil
{
    public static string MapsFolder => $"{Constants.USER_FOLDER}/maps";

    public static string MapsCacheFolder => $"{Constants.USER_FOLDER}/cache/maps";

    public static string? GetMapVideoPath(string mapPath)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(mapPath);

        FileInfo[] filesInfo = dirInfo.GetFiles();
        Dictionary<string, string> extPath = [];
        
        foreach (FileInfo file in filesInfo)
        {
            if (Path.GetFileNameWithoutExtension(file.FullName) == "video" && Constants.SUPPORTED_VIDEO_EXTENSIONS.Contains(file.Extension.TrimStart('.')))
            {
                extPath.TryAdd(file.Extension.TrimStart('.'), file.FullName);
            }
        }

        for (int i = 0; i < Constants.SUPPORTED_VIDEO_EXTENSIONS.Length; i++)
        {
            if (extPath.TryGetValue(Constants.SUPPORTED_VIDEO_EXTENSIONS[i], out string fullPath)) { return fullPath; }
        }

        return null;
    }

    public static string? GetMapVideoPath(Map map, bool skipMetadata = false)
    {
        string mapPath = map.FolderPath;

        if (!string.IsNullOrEmpty(map.VideoExt) && !skipMetadata)
        {
            return Path.Join(map.FolderPath, $"video.{map.VideoExt}");
        }

        return GetMapVideoPath(mapPath);
    }
}
