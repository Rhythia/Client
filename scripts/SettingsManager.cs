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
    public static ColorRect Menu;

    public static SettingsManager Instance { get; private set; }

    public SettingsProfile Settings = new SettingsProfile();

    [Signal]
    public delegate void MenuToggledEventHandler(bool shown);

    [Signal]
    public delegate void SavedEventHandler();

    [Signal]
    public delegate void LoadedEventHandler();

    public override void _Ready()
    {
        Instance = this;

        Menu = SceneManager.Instance.GetNode<ColorRect>("Settings");

        HideMenu();
    }

    public static void ShowMenu(bool show = true)
    {
        Shown = show;

        Instance.EmitSignal(SignalName.MenuToggled, Shown);
    }

    public static void HideMenu()
    {
        ShowMenu(false);
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

        void addUserContentToSettingsList(SettingsItem<string> settingsItem, IEnumerable<string> options)
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
}
