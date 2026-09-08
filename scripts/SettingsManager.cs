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

    public static string GetUserFolder()
    {
        if (!File.Exists(Constants.USER_FOLDER_POINTER) || string.IsNullOrWhiteSpace(File.ReadAllText(Constants.USER_FOLDER_POINTER)))
        {
            File.WriteAllText(Constants.USER_FOLDER_POINTER, Constants.DEFAULT_USER_FOLDER);
        }

        string path = File.ReadAllText(Constants.USER_FOLDER_POINTER).Trim();
        
        try
        {
            var _ = Directory.EnumerateFileSystemEntries(path);
        }
        catch (Exception exception)
        {
            File.WriteAllText(Constants.USER_FOLDER_POINTER, Constants.DEFAULT_USER_FOLDER);
            Logger.Error(exception);
            return Constants.DEFAULT_USER_FOLDER;
        }

        return path;
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

        File.WriteAllText(Constants.USER_FOLDER_POINTER, path);

        var popup = new OptionPopup("Restart required.", "Would you like to restart the game?");

        popup.AddOption("Restart", Callable.From(() => {
            string executablePath = OS.GetExecutablePath();
            OS.CreateProcess(executablePath, []);  // may misbehave on macos
            Instance.GetTree().Quit();
        }));
        popup.AddOption("Cancel", Callable.From(popup.Hide));

        SettingsMenu.Instance.Hide();
        popup.Show();
    }

    // the HideNotifications bool exists to prevent a lot of toasts that inform the user of changing the skin to "default",
    // this bool is only used inside of SkinManager - line 164.
    public static void ResetToDefaults()
    {
        HideNotifications = true;

        SettingsProfile defaults = new SettingsProfile();

        foreach (var property in typeof(SettingsProfile).GetProperties())
        {
            if (!typeof(ISettingsItem).IsAssignableFrom(property.PropertyType)) continue;

            ISettingsItem current = (ISettingsItem)property.GetValue(Instance.Settings);
            ISettingsItem defs = (ISettingsItem)property.GetValue(defaults);

            current.SetVariant(defs.GetVariant());
        }

        Save();
        HideNotifications = false;

        ToastNotification.Notify("Settings reset to default successfully!");
    }
}
