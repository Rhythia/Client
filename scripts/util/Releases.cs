using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Octokit;
using Updatum;

public class Releases
{
    public static Version GetCurrentVersion => Version.Parse((string)ProjectSettings.GetSetting("application/config/version"));

    public static readonly UpdatumManager MANAGER = new("Rhythia", "Client", currentVersion: GetCurrentVersion)
    {
        AssetRegexPattern = $"{OS.GetName()}",
    };

    // TODO: Add update request in menus when updates are found
    public static void Initialize()
    {
        MANAGER.AutoUpdateCheckTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
    }

    public static async Task<bool> CheckForUpdatesAsync() => await MANAGER.CheckForUpdatesAsync();

    public static async void UpdateToLatest()
    {
        var release = await DownloadReleaseAsync();

        await InstallUpdateAsync(release);
    }

    public static async Task<UpdatumDownloadedAsset> DownloadReleaseAsync(Release release = null)
    {
        release ??= MANAGER.LatestRelease;

        if (release == null)
        {
            return null;
        }

        var asset = await MANAGER.DownloadUpdateAsync(release);
        return asset;
    }

    public static async Task<bool> InstallUpdateAsync(UpdatumDownloadedAsset asset)
    {
        if (OS.HasFeature("editor") || OS.HasFeature("debug"))
        {
            Logger.Error("Can not install update while in editor/debug");
            return false;
        }

        return await MANAGER.InstallUpdateAsync(asset);
    }
}
