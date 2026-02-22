using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SettingsProfile
{
    #region Gameplay

    /// <summary>
    /// Adjusts cursor sensitivity
    /// </summary>
    [Order]
    public SettingsItem<float> Sensitivity { get; private set; }

    /// <summary>
    /// Toggles absolute input
    /// </summary>
    [Order]
    public SettingsItem<bool> AbsoluteInput { get; private set; }

    /// <summary>
    /// Toggles cursor drift
    /// </summary>
    [Order]
    public SettingsItem<bool> CursorDrift { get; private set; }

    /// <summary>
    /// Approach rate of hit objects
    /// </summary>
    [Order]
    public SettingsItem<float> ApproachRate { get; private set; }

    /// <summary>
    /// Approach distance of hit objects
    /// </summary>
    [Order]
    public SettingsItem<float> ApproachDistance { get; private set; }

    /// <summary>
    /// Approach time of hit objects
    /// </summary>
    [Order]
    public SettingsItem<float> ApproachTime { get; private set; }

    /// <summary>
    /// Distance for the hit objects to become fully opaqu
    /// </summary>
    [Order]
    public SettingsItem<float> FadeIn { get; private set; }

    /// <summary>
    /// Toggles fade out for the hit objects
    /// </summary>
    [Order]
    public SettingsItem<bool> FadeOut { get; private set; }

    /// <summary>
    /// Toggles hit object pushback
    /// </summary>
    [Order]
    public SettingsItem<bool> Pushback { get; private set; }

    /// <summary>
    /// Adjusts the camera parallax
    /// </summary>
    [Order]
    public SettingsItem<float> CameraParallax { get; private set; }

    /// <summary>
    /// Adjusts the HUD parallax
    /// </summary>
    [Order]
    public SettingsItem<float> HUDParallax { get; private set; }

    /// <summary>
    /// Adjusts the Field of View
    /// </summary>
    [Order]
    public SettingsItem<float> FoV { get; private set; }

    #endregion

    #region Visual

    /// <summary>
    /// Selected skin for the game
    /// </summary>
    [Order]
    public SettingsItem<string> Skin { get; private set; }

    /// <summary>
    /// Overrides the skin's background space for the menu
    /// </summary>
    [Order]
    public SettingsItem<string> MenuSpace { get; private set; }

    /// <summary>
    /// Overrides the skin's background space for the game
    /// </summary>
    [Order]
    public SettingsItem<string> GameSpace { get; private set; }

    /// <summary>
    /// Toggles note hit effects for the game space
    /// </summary>
    [Order]
    public SettingsItem<bool> SpaceHitEffects { get; private set; }

    /// <summary>
    /// Overrides the skin's colorset
    /// </summary>
    [Order]
    public SettingsItem<string> NoteColors { get; private set; }

    /// <summary>
    /// Sets the maximum opacity of the notes
    /// </summary>
    [Order]
    public SettingsItem<float> NoteOpacity { get; private set; }

    /// <summary>
    /// Overrides the skin's note mesh
    /// </summary>
    [Order]
    public SettingsItem<string> NoteMesh { get; private set; }

    /// <summary>
    /// Sets the size of the notes
    /// </summary>
    [Order]
    public SettingsItem<float> NoteSize { get; private set; }

    /// <summary>
    /// Adjusts the cursor scale
    /// </summary>
    [Order]
    public SettingsItem<float> CursorScale { get; private set; }

    /// <summary>
    /// Degrees to rotate the cursor by every second
    /// </summary>
    [Order]
    public SettingsItem<int> CursorRotation { get; private set; }

    /// <summary>
    /// Toggles a trial for your cursor
    /// </summary>
    [Order]
    public SettingsItem<bool> CursorTrail { get; private set; }

    /// <summary>
    /// Adjusts trail visibility time
    /// </summary>
    [Order]
    public SettingsItem<float> TrailTime { get; private set; }

    /// <summary>
    /// Adjusts the detail for the trail
    /// </summary>
    [Order]
    public SettingsItem<float> TrailDetail { get; private set; }

    /// <summary>
    /// Uses the skin's cursor instead of the native cursor
    /// </summary>
    [Order]
    public SettingsItem<bool> UseCursorInMenus { get; private set; }

    /// <summary>
    /// Adjusts the video background dim
    /// </summary>
    [Order]
    public SettingsItem<float> VideoDim { get; private set; }

    /// <summary>
    /// Adjusts the scale of the video background
    /// </summary>
    [Order]
    public SettingsItem<float> VideoRenderScale { get; private set; }

    /// <summary>
    /// Toggles a minimal HUD
    /// </summary>
    [Order]
    public SettingsItem<bool> SimpleHUD { get; private set; }

    /// <summary>
    /// Toggles a popup on a hit
    /// </summary>
    [Order]
    public SettingsItem<bool> HitPopups { get; private set; }

    /// <summary>
    /// Toggles a popup on a miss
    /// </summary>
    [Order]
    public SettingsItem<bool> MissPopups { get; private set; }

    #endregion

    #region Video

    /// <summary>
    /// Toggles the window to Fullsceen
    /// </summary>
    [Order]
    public SettingsItem<bool> Fullscreen { get; private set; }

    /// <summary>
    /// Unlocks maximum frames per second
    /// </summary>
    [Order]
    public SettingsItem<bool> UnlockFPS { get; private set; }

    /// <summary>
    /// Adjusts maximum frames per second
    /// </summary>
    [Order]
    public SettingsItem<int> FPS { get; private set; }


    #endregion

    #region Audio

    /// <summary>
    /// Master control for the audio
    /// </summary>
    [Order]
    public SettingsItem<float> VolumeMaster { get; private set; }

    /// <summary>
    /// Audio control for the music
    /// </summary>
    [Order]
    public SettingsItem<float> VolumeMusic { get; private set; }

    /// <summary>
    /// Audio control for sound effects
    /// </summary>
    [Order]
    public SettingsItem<float> VolumeSFX { get; private set; }

    /// <summary>
    /// Toggles hit sound to always play
    /// </summary>
    [Order]
    public SettingsItem<bool> AlwaysPlayHitSound { get; private set; }

    /// <summary>
    /// Automatically plays the jukebox on start
    /// </summary>
    [Order]
    public SettingsItem<bool> AutoplayJukebox { get; private set; }

    #endregion

    #region Other

    // [Order]
    /// <summary>
    /// Import settings from previous (nightly) version
    /// </summary>
    // public SettingsItem<Variant> RhythiaImport { get; private set; }

    [Order]
    /// <summary>
    /// Toggles recording for replays
    /// </summary>
    public SettingsItem<bool> RecordReplays { get; private set; }

    [Order]
    /// <summary>
    /// Restarts settings to the game's defaults
    /// </summary>
    public SettingsItem<Variant> ResetToDefaults { get; private set; }
    #endregion

    public SettingsProfile()
    {
        #region Initializers

        Sensitivity = new(0.5f)
        {
            Id = "Sensitivity",
            Title = "Sensitivity",
            Description = "Adjusts cursor sensitivity",
            Section = SettingsSection.Gameplay,
            Slider = new()
            {
                Step = 0.01f,
                MinValue = 0.01f,
                MaxValue = 2.5f
            },
        };

        AbsoluteInput = new(false)
        {
            Id = "AbsoluteInput",
            Title = "Absolute Input",
            Description = "Toggles absolute inputs",
            Section = SettingsSection.Gameplay,
        };

        ApproachRate = new(32)
        {
            Id = "ApproachRate",
            Title = "Approach Rate",
            Description = "Approach rate of hit objects",
            Section = SettingsSection.Gameplay,
            UpdateAction = _ => updateApproachTime(),
            Slider = new()
            {
                Step = 0.5f,
                MinValue = 0.5f,
                MaxValue = 100
            }
        };

        ApproachDistance = new(20)
        {
            Id = "ApproachDistance",
            Title = "Approach Distance",
            Description = "Approach distance of hit objects",
            Section = SettingsSection.Gameplay,
            UpdateAction = _ => updateApproachTime(),
            Slider = new()
            {
                Step = 0.5f,
                MinValue = 0.5f,
                MaxValue = 100
            }
        };

        ApproachTime = new(default)
        {
            Id = "ApproachTime",
            Title = "Approach Time",
            Description = "Approach time of hit objects",
            Section = SettingsSection.Gameplay,
            Visible = false,
            SaveToDisk = false
        };

        CursorDrift = new(true)
        {
            Id = "CursorDrift",
            Title = "Cursor Drift",
            Description = "Toggles cursor drift",
            Section = SettingsSection.Gameplay,
        };

        FadeIn = new(15)
        {
            Id = "FadeIn",
            Title = "Fade In",
            Description = "Distance for the hit objects to become fully opaque",
            Section = SettingsSection.Gameplay,
            Slider = new()
            {
                Step = 1,
                MinValue = 0,
                MaxValue = 100
            }
        };

        FadeOut = new(true)
        {
            Id = "FadeOut",
            Title = "Fade Out",
            Description = "Toggles fade out for the hit objects",
            Section = SettingsSection.Gameplay,
        };

        Pushback = new(true)
        {
            Id = "Pushback",
            Title = "Pushback",
            Description = "Toggles hit object pushback",
            Section = SettingsSection.Gameplay,
        };

        CameraParallax = new(0.1f)
        {
            Id = "CameraParallax",
            Title = "Camera Parallax",
            Description = "Adjusts the camera parallax",
            Section = SettingsSection.Gameplay,
            Slider = new()
            {
                Step = 0.05f,
                MinValue = 0,
                MaxValue = 1
            }
        };

        HUDParallax = new(0)
        {
            Id = "HUDParallax",
            Title = "HUD Parallax",
            Description = "(Not implemented) Adjusts the HUD parallax",
            Section = SettingsSection.Gameplay,
            Slider = new()
            {
                Step = 0.05f,
                MinValue = 0,
                MaxValue = 1
            }
        };

        FoV = new(70)
        {
            Id = "FoV",
            Title = "Field of View",
            Description = "Adjusts the field of view",
            Section = SettingsSection.Gameplay,
            Slider = new()
            {
                Step = 1,
                MinValue = 60,
                MaxValue = 120,
            }
        };

        Skin = new("default")
        {
            Id = "Skin",
            Title = "Skin",
            Description = "Selected skin for the game",
            Section = SettingsSection.Visual,
            UpdateAction = _ => SkinManager.Load(),
            Buttons =
            [
                new() { Title = "Skin Folder", Description = "Open the skin folder", OnPressed = () => { OS.ShellOpen($"{Constants.USER_FOLDER}/skins/{SettingsManager.Instance.Settings.Skin}"); } }
            ],
            List = new("default")
            {
                Values = [ "default" ]
            }
        };

        MenuSpace = new("skin")
        {
            Id = "MenuSpace",
            Title = "Menu Space",
            Description = "Overrides the skin's background space for the menu",
            Section = SettingsSection.Visual,
            UpdateAction = _ => SkinManager.Load(),
            List = new("skin")
            {
                Values = [ "skin", "void", "grid", "squircles", "waves" ]
            }
        };

        GameSpace = new("skin")
        {
            Id = "GameSpace",
            Title = "Game Space",
            Description = "Overrides the skin's background space for gameplay",
            Section = SettingsSection.Visual,
            UpdateAction = _ => SkinManager.Load(),
            List = new("skin")
            {
                Values = [ "skin", "void", "grid", "squircles", "waves" ]
            }
        };

        SpaceHitEffects = new(true)
        {
            Id = "SpaceHitEffects",
            Title = "Space Hit Effects",
            Description = "Toggles note hit effects for the game space",
            Section = SettingsSection.Visual
        };

        NoteColors = new("skin")
        {
            Id = "Colors",
            Title = "Colors",
            Description = "Overrides the skin's colorset",
            Section = SettingsSection.Visual,
            UpdateAction = _ => SkinManager.Load(),
            List = new("skin")
            {
                Values = [ "skin", "default" ]
            }
        };

        NoteOpacity = new(1)
        {
            Id = "NoteOpacity",
            Title = "Note Opacity",
            Description = "Sets the maximum opacity for the notes",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 0.05f,
                MinValue = 0,
                MaxValue = 1
            }
        };

        NoteMesh = new("skin")
        {
            Id = "NoteMesh",
            Title = "Note Mesh",
            Description = "Overrides the skin's note mesh",
            Section = SettingsSection.Visual,
            UpdateAction = _ => SkinManager.Load(),
            List = new("skin")
            {
                Values = [ "skin", "squircle", "square" ]
            }
        };

        NoteSize = new(0.875f)
        {
            Id = "NoteSize",
            Title = "Note Size",
            Description = "Sets the size of the notes",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 0.025f,
                MinValue = 0,
                MaxValue = 2
            }
        };

        CursorScale = new(1)
        {
            Id = "CursorScale",
            Title = "Cursor Scale",
            Description = "Adjusts the cursor scale",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 0.025f,
                MinValue = 0,
                MaxValue = 4
            }
        };

        CursorRotation = new(0)
        {
            Id = "CursorRotation",
            Title = "Cursor Rotation",
            Description = "Degrees to rotate the cursor by every second",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 1,
                MinValue = -360,
                MaxValue = 360
            }
        };

        CursorTrail = new(false)
        {
            Id = "CursorTrail",
            Title = "Cursor Trail",
            Description = "Toggles a trail for your cursor",
            Section = SettingsSection.Visual
        };

        TrailTime = new(0.05f)
        {
            Id = "TrailTime",
            Title = "Trail Time",
            Description = "Adjusts trail visibility time",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 0.01f,
                MinValue = 0,
                MaxValue = 0.5f
            }
        };

        TrailDetail = new(1)
        {
            Id = "TrailDetail",
            Title = "Trail Detail",
            Description = "(Not implemented) Adjusts the detail for the trail",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 0.05f,
                MinValue = 0,
                MaxValue = 5
            }
        };

        UseCursorInMenus = new(false)
        {
            Id = "UseCursorInMenus",
            Title = "Use Cursor in Menus",
            Description = "Uses the skin's cursor instead of the native cursor",
            Section = SettingsSection.Visual
        };

        VideoDim = new(80)
        {
            Id = "VideoDim",
            Title = "Video BG Dim",
            Description = "Adjusts the video background dim",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 1,
                MinValue = 0,
                MaxValue = 100
            }
        };

        VideoRenderScale = new(100)
        {
            Id = "VideoRenderScale",
            Title = "Video BG Render Scale",
            Description = "Adjusts the scale of the video background",
            Section = SettingsSection.Visual,
            Slider = new()
            {
                Step = 1,
                MinValue = 0,
                MaxValue = 100
            }
        };

        SimpleHUD = new(false)
        {
            Id = "SimpleHUD",
            Title = "Simple HUD",
            Description = "Toggles a minimal HUD",
            Section = SettingsSection.Visual,
        };

        HitPopups = new(true)
        {
            Id = "HitPopups",
            Title = "Hit Score Popups",
            Description = "Toggles a popup on a hit",
            Section = SettingsSection.Visual,
        };

        MissPopups = new(true)
        {
            Id = "MissPopups",
            Title = "Miss Popups",
            Description = "Toggles a popup on a miss",
            Section = SettingsSection.Visual,
        };

        Fullscreen = new(true)
        {
            Id = "Fullscreen",
            Title = "Fullscreen",
            Description = "Toggles the window to fullscreen",
            Section = SettingsSection.Video,
            UpdateAction = value => DisplayServer.WindowSetMode(
                value
                ? DisplayServer.WindowMode.ExclusiveFullscreen
                : DisplayServer.WindowMode.Windowed)
        };

        UnlockFPS = new(true)
        {
            Id = "UnlockFPS",
            Title = "Unlock FPS",
            Description = "Unlocks maximum frames per second",
            Section = SettingsSection.Video,
            UpdateAction = value => Engine.MaxFps = UnlockFPS ? 0 : FPS
        };

        FPS = new(240)
        {
            Id = "FPS",
            Title = "FPS",
            Description = "Adjusts maximum frames per second",
            Section = SettingsSection.Video,
            Slider = new()
            {
                Step = 5,
                MinValue = 60,
                MaxValue = 540,
            },
            UpdateAction = value => Engine.MaxFps = UnlockFPS ? 0 : FPS
        };

        AutoplayJukebox = new(true)
        {
            Id = "AutoplayJukebox",
            Title = "Autoplay Jukebox",
            Description = "Automatically plays the jukebox on start",
            Section = SettingsSection.Audio,
        };

        AlwaysPlayHitSound = new(false)
        {
            Id = "AlwaysPlayHitSound",
            Title = "Always Play Hit Sound",
            Description = "Toggles hit sound to always play",
            Section = SettingsSection.Audio,
        };

        VolumeMaster = new(50)
        {
            Id = "VolumeMaster",
            Title = "Master Volume",
            Description = "Master volume control for all audio",
            Section = SettingsSection.Audio,
            UpdateAction = _ => SoundManager.UpdateVolume(),
            Slider = new()
            {
                Step = 1,
                MinValue = 0,
                MaxValue = 100
            }
        };

        VolumeMusic = new(50)
        {
            Id = "VolumeMusic",
            Title = "Music Volume",
            Description = "Audio control for the music",
            Section = SettingsSection.Audio,
            UpdateAction = _ => SoundManager.UpdateVolume(),
            Slider = new()
            {
                Step = 1,
                MinValue = 0,
                MaxValue = 100
            }
        };

        VolumeSFX = new(50)
        {
            Id = "VolumeSFX",
            Title = "SFX Volume",
            Description = "Audio control for sound effects",
            Section = SettingsSection.Audio,
            UpdateAction = _ => SoundManager.UpdateVolume(),
            Slider = new()
            {
                Step = 1,
                MinValue = 0,
                MaxValue = 100
            }
        };

        // RhythiaImport = new(default)
        // {
        //     Id = "RhythiaImport",
        //     Title = "Import Nightly Settings",
        //     Description = "Imports settings from the nightly client",
        //     Section = SettingsSection.Other,
        //     Buttons =
        //     [
        //         new() { Title = "Import", Description = "", OnPressed = () => { } }
        //     ],
        //     SaveToDisk = false,
        // };

        RecordReplays = new(true)
        {
            Id = "RecordReplays",
            Title = "Record Replays",
            Description = "Toggles recording for replays",
            Section = SettingsSection.Other
        };

        ResetToDefaults = new(default)
        {
            Id = "ResetToDefaults",
            Title = "Reset to Defaults",
            Description = "Resets all settings to default values.",
            Section = SettingsSection.Other,
            Buttons = 
            [
                new() {Title = "Reset", Description = "WARNING: THIS RESETS YOUR CURRENT PROFILE!", OnPressed = () => {
                    SettingsManager.ResetToDefaults();
                }}
            ],
        };

        #endregion

        updateApproachTime();
    }

    /// <summary>
    /// Orders all the <see cref="SettingsItem{T}"/> that is present in the <see cref="SettingsProfile"/>
    /// into a dictionary dependent of their <see cref="SettingsSection"/>
    /// </summary>
    /// <returns>Dictionary of Lists that has ordered <see cref="SettingsItem{T}"/></returns>
    public Dictionary<SettingsSection, List<ISettingsItem>> ToOrderedSectionList()
    {
        var dictionary = new Dictionary<SettingsSection, List<ISettingsItem>>();

        foreach (SettingsSection section in Enum.GetValues(typeof(SettingsSection)))
        {
            dictionary.Add(section, new List<ISettingsItem>());
        }

        var items = typeof(SettingsProfile).GetProperties()
            .Where(p => typeof(ISettingsItem).IsAssignableFrom(p.PropertyType))
            .Where(p => Attribute.IsDefined(p, typeof(OrderAttribute)))
            .OrderBy
            (
                p => ((OrderAttribute)p
                .GetCustomAttributes(typeof(OrderAttribute), false)
                .Single()).Order
            )
            .Select(p => (ISettingsItem)p.GetValue(this))
            .ToList();

        foreach (var item in items)
        {
            dictionary[item.Section].Add(item);
        }

        return dictionary;
    }

    private void updateApproachTime()
    {
        ApproachTime.Value = ApproachDistance / ApproachRate;
    }
}
