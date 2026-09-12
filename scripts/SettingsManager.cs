using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SettingsManager : Node
{
    public static bool Shown = false;

    public static bool HideNotifications = false;

    public static SettingsMenu Menu;

    public static SettingsManager Instance { get; private set; }

    public SettingsProfile Settings = new SettingsProfile();

    [Signal]
    public delegate void SavedEventHandler();

    [Signal]
    public delegate void LoadedEventHandler();

    public override void _Ready()
    {
        Instance = this;

        Menu = SceneManager.Instance.GetNode<SettingsMenu>("Settings");
    }

    public static void Save(string profile = null)
    {
        profile ??= GetCurrentProfile();

        string data = SettingsProfileConverter.Serialize(Instance.Settings);

        File.WriteAllText($"{Constants.USER_FOLDER}/profiles/{profile}.json", data);

        Logger.Log($"Saved settings {profile}");

        Instance.EmitSignal(SignalName.Saved);

        SkinManager.Save();
    }

    public static void Load(string profile = null)
    {
        profile ??= GetCurrentProfile();

        try
        {
            SettingsProfileConverter.Deserialize($"{Constants.USER_FOLDER}/profiles/{profile}.json", Instance.Settings);

            ToastNotification.Notify($"Loaded profile [{profile}]");
        }
        catch (Exception exception)
        {
            ToastNotification.Notify("Settings file corrupted", 2);
            Logger.Error(exception);
        }

        if (!Directory.Exists($"{Constants.USER_FOLDER}/skins/{Instance.Settings.Skin.Value}"))
        {
            Instance.Settings.Skin.Value = new("default");
            ToastNotification.Notify($"Could not find skin {Instance.Settings.Skin.Value}", 1);
        }

        static void addUserContentToSettingsList(SettingsItem<string> settingsItem, IEnumerable<string> options)
        {
            foreach (string option in options)
            {
                string name = option.GetFile().GetBaseName();

                if (settingsItem.List.Values.IndexOf(name) == -1)
                {
                    settingsItem.List.Values.Add(name);
                }
            }
        }

        addUserContentToSettingsList(Instance.Settings.Skin, Directory.GetDirectories($"{Constants.USER_FOLDER}/skins"));
        addUserContentToSettingsList(Instance.Settings.NoteColors, Directory.GetFiles($"{Constants.USER_FOLDER}/colorsets"));

        Logger.Log($"Loaded settings {profile}");

        Instance.EmitSignal(SignalName.Loaded);

        SkinManager.Load();
    }

    public static void Reload()
    {
        Save();
        Load();
    }

    public static void SetCurrentProfile(string profile = null)
    {
        profile ??= GetCurrentProfile();

        File.WriteAllText($"{Constants.USER_FOLDER}/current_profile.txt", profile);
    }

    public static string GetCurrentProfile()
    {
        string file = $"{Constants.USER_FOLDER}/current_profile.txt";

        if (File.Exists(file))
        {
            return File.ReadAllText(file);
        }

        return "default";
    }

    public static string[] GetUserFolderPointerContent()
    {
        string currentDir = "";
        string previousDir = "";

        if (!File.Exists(Constants.USER_FOLDER_POINTER) || string.IsNullOrWhiteSpace(File.ReadAllText(Constants.USER_FOLDER_POINTER)))
        {
            currentDir = Constants.DEFAULT_USER_FOLDER;
            File.WriteAllText(Constants.USER_FOLDER_POINTER, $"CurrentDir:{Constants.DEFAULT_USER_FOLDER}\nPreviousDir:");
            return [currentDir, previousDir];
        }

        string pointerContent = File.ReadAllText(Constants.USER_FOLDER_POINTER).Trim();
        string[] lines = pointerContent.Split("\n");
        
        foreach (string line in lines)
        {
            string normalLine = line.TrimEnd('\r');

            if (normalLine.StartsWith("CurrentDir:")) { currentDir = normalLine.Substring("CurrentDir:".Length); }
            else if (normalLine.StartsWith("PreviousDir:")) { previousDir = normalLine.Substring("PreviousDir:".Length); }
            else { Logger.Error("Unexpected line in user pointer file."); }
        }

        return [currentDir, previousDir];
    }

    public static string GetUserFolder()
    {
        string[] paths = GetUserFolderPointerContent();
        string currentDir = paths[0];
        
        try
        {
            var _ = Directory.EnumerateFileSystemEntries(currentDir);
        }
        catch (Exception exception)
        {
            paths[0] = Constants.DEFAULT_USER_FOLDER;
            File.WriteAllText(Constants.USER_FOLDER_POINTER, $"CurrentDir:{paths[0]}\nPreviousDir:{paths[1]}");
            Logger.Error(exception);
            return Constants.DEFAULT_USER_FOLDER;
        }

        return currentDir;
    }

    public static string GetPreviousUserFolder()
    {
        string[] paths = GetUserFolderPointerContent();
        string previousDir = paths[1];

        if (string.IsNullOrWhiteSpace(previousDir)) { return ""; }

        try
        {
            var _ = Directory.EnumerateFileSystemEntries(previousDir);
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            return "";
        }

        return previousDir;
    }

    public static void SetUserFolder(string path)
    {
        try
        {
            var _ = Directory.EnumerateFileSystemEntries(path);
        }
        catch (Exception exception)
        {
            ToastNotification.Notify("Invalid User Path. Setting has not been changed.", 2);
            Logger.Error(exception);
            return;
        }

        File.WriteAllText(Constants.USER_FOLDER_POINTER, $"CurrentDir:{path}\nPreviousDir:{Constants.USER_FOLDER}");

        var popup = new OptionPopup("Restart required.", "Would you like to restart the game?");

        popup.AddOption("Restart", Callable.From(restartGame));
        popup.AddOption("Restart And Copy", Callable.From(() => {
            popup.Hide();
            showUserFolderConfirmationPopup(path);
        }));
        popup.AddOption("Cancel", Callable.From(popup.Hide));

        SettingsMenu.Instance.Hide();
        popup.Show();
    }

    // the HideNotifications bool exists to prevent a lot of toasts that inform the user of changing the skin to "default",
    // this bool is only used inside of SkinManager - line 164.
    public static void ResetToDefaults()
    {
        string[] excludeFromReset = ["SetUserFolderPath", "SetUserFolderDialog"];

        HideNotifications = true;

        SettingsProfile defaults = new SettingsProfile();

        foreach (var property in typeof(SettingsProfile).GetProperties())
        {
            if (!typeof(ISettingsItem).IsAssignableFrom(property.PropertyType)) continue;
            if (excludeFromReset.Contains(property.Name)) continue;

            ISettingsItem current = (ISettingsItem)property.GetValue(Instance.Settings);
            ISettingsItem defs = (ISettingsItem)property.GetValue(defaults);

            current.SetVariant(defs.GetVariant());
        }

        Save();
        HideNotifications = false;

        ToastNotification.Notify("Settings reset to default successfully!");
    }
    private static void restartGame()
    {
        string executablePath = OS.GetExecutablePath();
        OS.CreateProcess(executablePath, []);  // may misbehave on macos
        Instance.GetTree().Quit();
    }

    private static void showUserFolderConfirmationPopup(string destinationPath)
    {
        var popup = new OptionPopup("Are you sure?", "This will overwrite the contents of the new folder and double the game in size unless you remove the old files.\n\nPS: you can find the old folder in settings > other > open old user folder");

        popup.AddOption("Restart And Copy", Callable.From(() => {
            FileOperations.CopyDir(Constants.USER_FOLDER, destinationPath, true);
            if (Path.GetFullPath(destinationPath) != Constants.DEFAULT_USER_FOLDER) { File.Delete(Path.Combine(destinationPath, "user_folder_path.txt")); }
            restartGame();
        }));
        popup.AddOption("Cancel", Callable.From(popup.Hide));

        popup.Show();
    }
}
