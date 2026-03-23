using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class SoundManager : Node, ISkinnable
{
    public static SoundManager Instance;

    public static AudioStreamPlayer HitSound;
    public static AudioStreamPlayer MissSound;
    public static AudioStreamPlayer FailSound;
    public static AudioStreamPlayer Song;

    public Action<Map> JukeboxPlayed;

    public event Action JukeboxEmpty;

    public static int[] JukeboxQueue = [];
    public static int JukeboxIndex = 0;
    public static bool JukeboxPaused = false;
    public static ulong LastRewind = 0;
    public static Map Map;

    private static bool volumePopupShown = false;
    private static ulong lastVolumeChange = 0;

    public override void _Ready()
    {
        Instance = this;

        HitSound = new();
        MissSound = new();
        FailSound = new();
        Song = new();

        HitSound.MaxPolyphony = 16;

        AddChild(HitSound);
        AddChild(MissSound);
        AddChild(FailSound);
        AddChild(Song);

        SkinManager.Instance.Loaded += UpdateSkin;

        UpdateSkin(SkinManager.Instance.Skin);

        Song.Finished += () =>
        {
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
        MapManager.Selected.ValueChanged += (_, selected) =>
        {
            var map = selected.Value;

            if (Map == null || Map.Name != map.Name)
            {
                PlayJukebox(map);
            }
        };

        MapManager.MapDeleted += (map) =>
        {
            UpdateJukeboxQueue();

            if (Map != map)
            {
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
        }

        if (MapManager.Initialized)
        {
            start();
            return;
        }

        MapManager.MapsInitialized += _ => start();
    }

    public override void _Process(double delta)
    {
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

    public static void UpdateVolume()
    {
        var settings = SettingsManager.Instance.Settings;

        Song.VolumeDb = -80 + 70 * (float)Math.Pow(settings.VolumeMusic.Value / 100, 0.1) * (float)Math.Pow(settings.VolumeMaster.Value / 100, 0.1);
        HitSound.VolumeDb = -80 + 80 * (float)Math.Pow(settings.VolumeSFX.Value / 100, 0.1) * (float)Math.Pow(settings.VolumeMaster.Value / 100, 0.1);
        FailSound.VolumeDb = -80 + 80 * (float)Math.Pow(settings.VolumeSFX.Value / 100, 0.1) * (float)Math.Pow(settings.VolumeMaster.Value / 100, 0.1);
    }

    public static void UpdateJukeboxQueue()
    {
        JukeboxQueue = [.. MapManager.Maps.Select(x => x.Id)];
    }

    public void UpdateSkin(SkinProfile skin)
    {
        HitSound.Stream = Util.Audio.LoadStream(skin.HitSoundBuffer);
        FailSound.Stream = Util.Audio.LoadStream(skin.FailSoundBuffer);
    }
}
