using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Octokit;
using Updatum;
using util;

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

        if (OS.GetName() == "macOS")
        {
            return installMacUpdate(asset);
        }

        return await MANAGER.InstallUpdateAsync(asset);
    }

    // Updatum finds the .app through AppContext.BaseDirectory, which Godot points at Contents/Resources,
    // so it would sync the update inside the running bundle instead of replacing it
    private static bool installMacUpdate(UpdatumDownloadedAsset asset)
    {
        if (asset == null || !asset.FileExists)
        {
            return false;
        }

        string app = Platform.ShellQuote(Platform.MacAppBundlePath);
        string zip = Platform.ShellQuote(asset.FilePath);

        string script =
            $"{Platform.WaitForExitScript}; "
            + $"tmp=$(mktemp -d) && ditto -x -k {zip} \"$tmp\" "
            + "&& new=$(find \"$tmp\" -maxdepth 1 -name '*.app' | head -n 1) && [ -n \"$new\" ] "
            + $"&& mv {app} \"$tmp/old.app\" && {{ ditto \"$new\" {app} || mv \"$tmp/old.app\" {app}; }}; "
            + $"xattr -dr com.apple.quarantine {app}; rm -rf \"$tmp\"; open {app}";

        OS.CreateProcess("/bin/sh", ["-c", script]);
        ((SceneTree)Engine.GetMainLoop()).Quit();

        return true;
    }
}
