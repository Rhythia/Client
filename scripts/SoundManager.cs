using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class SoundManager : Node, ISkinnable
{
    public enum PlaybackScope
    {
        Silent,
        Preview,
        GameplayResults,
    }

    public static SoundManager Instance;

    public static AudioStreamPlayer HitSound;
    public static AudioStreamPlayer MissSound;
    public static AudioStreamPlayer FailSound;
    public static AudioStreamPlayer Song;
    public static AudioStreamPlayer MenuMusic;

    public Action<Map> JukeboxPlayed;

    public event Action JukeboxEmpty;

    public static int[] JukeboxQueue = [];
    public static int JukeboxIndex = 0;
    public static bool JukeboxPaused = false;
    public static ulong LastRewind = 0;
    public static Map Map;
    public static PlaybackScope Scope = PlaybackScope.Silent;

    private static bool volumePopupShown = false;
    private static ulong lastVolumeChange = 0;
    private static bool? jeeping = null; // we're jeeping (last state of a song)

    public override void _Ready()
    {
        Instance = this;

        HitSound = new();
        MissSound = new();
        FailSound = new();
        Song = new();
        MenuMusic = new();

        HitSound.MaxPolyphony = 16;
        MissSound.MaxPolyphony = 16;

        AddChild(HitSound);
        AddChild(MissSound);
        AddChild(FailSound);
        AddChild(Song);
        AddChild(MenuMusic);

        SkinManager.Instance.Loaded += UpdateSkin;

        UpdateSkin(SkinManager.Instance.Skin);

        Song.Finished += () =>
        {
            if (isScopedPlayback())
            {
                StopScopedSession();
                return;
            }

            switch (SceneManager.Scene.Name)
            {
                case "SceneMenu":
                    if (SettingsManager.Instance.Settings.AutoplayJukebox)
                    {
                        JukeboxIndex++;
                        PlayJukebox(JukeboxIndex);
                    }
                    break;
                case "SceneResults":
                    PlayJukebox(JukeboxIndex);  // play skinnable results song here in the future
                    break;
                default:
                    break;
            }
        };

        SettingsManager.Instance.Loaded += UpdateVolume;
        Lobby.Instance.SpeedChanged += (speed) => { SoundManager.Song.PitchScale = (float)speed; };
        MapManager.Selected.ValueChanged += (_, _) => RefreshMenuMusicPlayback();

        MapManager.MapDeleted += (map) =>
        {
            UpdateJukeboxQueue();

            if (Map != map)
            {
                return;
            }

            if (isScopedPlayback())
            {
                StopScopedSession();
                return;
            }

            if (JukeboxQueue.Length == 0)
            {
                Song.Stop();
                Map = null;
                JukeboxEmpty?.Invoke();
            }
            else
            {
                PlayJukebox(new Random().Next(0, JukeboxQueue.Length));
            }
        };

        UpdateVolume();

        static void start()
        {
            UpdateJukeboxQueue();

            if (SettingsManager.Instance.Settings.AutoplayJukebox)
            {
                PlayJukebox(new Random().Next(0, JukeboxQueue.Length));
            }
            else
            {
                StopScopedSession();
            }
        }

        if (MapManager.Initialized)
        {
            start();
            printSongPlaybackState();
            return;
        }

        MapManager.MapsInitialized += _ => start();

        RefreshMenuMusicPlayback();
        printSongPlaybackState();
    }

    public override void _Process(double delta)
    {
        RefreshMenuMusicPlayback();
        printSongPlaybackState();

        if (volumePopupShown && Time.GetTicksMsec() - lastVolumeChange >= 1000)
        {
            volumePopupShown = false;

            Tween tween = SceneManager.VolumePanel.CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
            tween.TweenProperty(SceneManager.VolumePanel, "modulate", Color.FromHtml("ffffff00"), 0.25);
            tween.TweenProperty(SceneManager.VolumePanel.GetNode<Label>("Label"), "anchor_bottom", 1, 0.35);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        var settings = SettingsManager.Instance.Settings;

        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            if ((eventMouseButton.CtrlPressed || eventMouseButton.AltPressed) && (eventMouseButton.ButtonIndex == MouseButton.WheelUp || eventMouseButton.ButtonIndex == MouseButton.WheelDown))
            {
                switch (eventMouseButton.ButtonIndex)
                {
                    case MouseButton.WheelUp:
                        settings.VolumeMaster.Value = (float)Mathf.Min(100, Math.Round(settings.VolumeMaster) + 5);
                        break;
                    case MouseButton.WheelDown:
                        settings.VolumeMaster.Value = (float)Mathf.Max(0, Math.Round(settings.VolumeMaster) - 5);
                        break;
                }

                Label label = SceneManager.VolumePanel.GetNode<Label>("Label");
                label.Text = settings.VolumeMaster.Value.ToString();

                Tween tween = SceneManager.VolumePanel.CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
                tween.TweenProperty(SceneManager.VolumePanel, "modulate", Color.FromHtml("ffffffff"), 0.25);
                tween.TweenProperty(SceneManager.VolumePanel.GetNode<ColorRect>("Main"), "anchor_right", settings.VolumeMaster.Value / 100, 0.15);
                tween.TweenProperty(label, "anchor_bottom", 0, 0.15);

                volumePopupShown = true;
                lastVolumeChange = Time.GetTicksMsec();

                UpdateVolume();
            }
        }
    }

    public static void PlayJukebox(Map map, bool setRichPresence = true)
    {
        if (map == null)
        {
            return;
        }

        if (isScopedPlayback())
        {
            StartMapSelectionPlayback(map, setRichPresence);
            return;
        }

        MenuMusic?.Stop();

        Map = map;

        if (map.AudioBuffer == null)
        {
            JukeboxIndex++;
            PlayJukebox(JukeboxIndex);
            return;
        }

        JukeboxIndex = MapManager.Maps.FindIndex(x => x.Id == map.Id);

        Song.Stream = Util.Audio.LoadFromFile($"{MapUtil.MapsCacheFolder}/{map.Name}/audio.{map.AudioExt}");
        Song.Play();

        Instance.JukeboxPlayed?.Invoke(map);

        if (setRichPresence)
        {
            Discord.Client.UpdateState($"Listening to {map.PrettyTitle}");
        }
    }

    public static void PlayJukebox(int index = -1, bool setRichPresence = true)
    {
        if (JukeboxQueue.Length == 0)
        {
            return;
        }

        index = index == -1 ? JukeboxIndex : index;

        if (index >= JukeboxQueue.Length)
        {
            index = 0;
        }
        else if (index < 0)
        {
            index = JukeboxQueue.Length - 1;
        }

        var map = MapManager.GetMapById(JukeboxQueue[index]);

        PlayJukebox(map, setRichPresence);
    }

    public static void StartMapSelectionPlayback(Map map, bool setRichPresence = true)
    {
        if (map == null)
        {
            return;
        }

        Song.Stop();
        Song.StreamPaused = false;
        Song.PitchScale = (float)Lobby.Speed;
        MenuMusic?.Stop();

        Map = map;
        Scope = PlaybackScope.Preview;

        if (MapManager.Maps != null)
        {
            int mapIndex = MapManager.Maps.FindIndex(x => x.Id == map.Id);
            if (mapIndex >= 0)
            {
                JukeboxIndex = mapIndex;
            }
        }

        Song.Stream = Util.Audio.LoadFromFile($"{MapUtil.MapsCacheFolder}/{map.Name}/audio.{map.AudioExt}");
        Song.Play(0);

        Instance.JukeboxPlayed?.Invoke(map);

        if (setRichPresence)
        {
            Discord.Client.UpdateState($"Listening to {map.PrettyTitle}");
        }
    }

    public static void BeginGameplayScope(Map map)
    {
        if (!isScopedPlayback())
        {
            return;
        }

        Map = map;
        Scope = PlaybackScope.GameplayResults;
        MenuMusic?.Stop();
    }

    public static void StopScopedSession()
    {
        Song.Stop();
        Song.StreamPaused = false;
        Map = null;
        Scope = PlaybackScope.Silent;
        Instance.JukeboxEmpty?.Invoke();

        RefreshMenuMusicPlayback();
    }

    private static bool isScopedPlayback()
    {
        return !SettingsManager.Instance.Settings.AutoplayJukebox.Value;
    }

    public static float ComputeVolumeDb(float volume, float master, float range)
    {
        if (volume <= 0 || master <= 0) return float.NegativeInfinity;
        return -80 + range * (float)Math.Pow(volume / 100, 0.1) * (float)Math.Pow(master / 100, 0.1);
    }

    public static void UpdateVolume()
    {
        var settings = SettingsManager.Instance.Settings;

        Song.VolumeDb = ComputeVolumeDb(settings.VolumeMusic.Value, settings.VolumeMaster.Value, 70);
        MenuMusic.VolumeDb = ComputeVolumeDb(settings.VolumeMenuMusic.Value, settings.VolumeMaster.Value, 70);
        HitSound.VolumeDb = ComputeVolumeDb(settings.VolumeHitSound.Value, settings.VolumeMaster.Value, 80);
        MissSound.VolumeDb = ComputeVolumeDb(settings.VolumeMissSound.Value, settings.VolumeMaster.Value, 80);
        FailSound.VolumeDb = ComputeVolumeDb(settings.VolumeSFX.Value, settings.VolumeMaster.Value, 80);
    }

    public static void PlayHitSound()
    {
        if (!isSoundEffectEnabled(SettingsManager.Instance?.Settings.EnableHitSound, HitSound))
        {
            return;
        }

        HitSound.Play();
    }

    public static void PlayMissSound()
    {
        if (!isSoundEffectEnabled(SettingsManager.Instance?.Settings.EnableMissSound, MissSound))
        {
            return;
        }

        MissSound.Play();
    }

    public static void UpdateJukeboxQueue()
    {
        JukeboxQueue = [.. MapManager.Maps.Select(x => x.Id)];
    }

    public void UpdateSkin(SkinProfile skin)
    {
        HitSound.Stream = Util.Audio.LoadStream(skin.HitSoundBuffer);
        MissSound.Stream = Util.Audio.LoadStream(skin.MissSoundBuffer);
        FailSound.Stream = Util.Audio.LoadStream(skin.FailSoundBuffer);
        MenuMusic.Stream = Util.Audio.LoadStream(skin.MenuMusicBuffer);

        RefreshMenuMusicPlayback();
    }

    public static void RefreshMenuMusicPlayback()
    {
        if (MenuMusic == null)
        {
            return;
        }

        if (shouldPlayMenuMusic())
        {
            if (!MenuMusic.Playing)
            {
                MenuMusic.Play();
            }
        }
        else if (MenuMusic.Playing)
        {
            MenuMusic.Stop();
        }
    }

    private static bool shouldPlayMenuMusic()
    {
        if (!SettingsManager.Instance.Settings.EnableMenuMusic.Value)
        {
            return false;
        }

        if (MenuMusic.Stream == null)
        {
            return false;
        }

        if (SceneManager.Scene is not MainMenu)
        {
            return false;
        }

        if (Song != null && Song.Playing)
        {
            return false;
        }

        return true;
    }

    private static bool isSoundEffectEnabled(SettingsItem<bool> setting, AudioStreamPlayer player)
    {
        return setting != null && setting.Value && player?.Stream != null;
    }

    private static void printSongPlaybackState()
    {
        bool isSongPlaying = Song != null && Song.Playing;

        if (jeeping == isSongPlaying) // vroom vroom jeep
        {
            return;
        }

        jeeping = isSongPlaying; //jeeps go beep beep
    }
}
