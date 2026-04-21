using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Godot;

public partial class LegacyRunner : BaseScene
{
    private static SettingsProfile settings;

    private static Node node;
    private static readonly PackedScene hit_feedback = GD.Load<PackedScene>("res://prefabs/hit_popup.tscn");
    private static readonly PackedScene miss_feedback = GD.Load<PackedScene>("res://prefabs/miss_icon.tscn");
    private static readonly PackedScene modifier_icon = GD.Load<PackedScene>("res://prefabs/modifier.tscn");

    public static Camera3D Camera;

    private static Panel menu;
    //private static Label fpsCounter;
    private static Label3D titleLabel;
    private static Label3D comboLabel;
    private static Label3D speedLabel;
    private static Label3D skipLabel;
    private static Label3D progressLabel;
    //private static TextureRect jesus;
    private static MeshInstance3D cursor;
    private static MeshInstance3D grid;
    private static MeshInstance3D videoQuad;
    private static MultiMeshInstance3D notesMultimesh;
    private static MultiMeshInstance3D cursorTrailMultimesh;
    private static TextureRect healthTexture;
    private static Node healthPanel;
    private static Node progressBarNode;
    private static TextureRect progressBarTexture;
    private static SubViewport panelLeft;
    private static SubViewport panelRight;
    //private static AudioStreamPlayer bell;
    private static Panel replayViewer;
    private static TextureButton replayViewerPause;
    private static Label replayViewerLabel;
    private static HSlider replayViewerSeek;
    private static Label accuracyLabel;
    private static Label hitsLabel;
    private static Label missesLabel;
    private static Label sumLabel;
    private static Label simpleMissesLabel;
    private static Label scoreLabel;
    private static Label multiplierLabel;
    private static Label pauseCountLabel;
    private static Panel multiplierProgressPanel;
    private static ShaderMaterial multiplierProgressMaterial;
    private static float multiplierProgress = 0;    // more efficient than spamming material.GetShaderParameter()
    private static Color multiplierColour = Color.Color8(255, 255, 255);
    private static VideoStreamPlayer video;
    private static Tween hitTween;
    private static Tween missTween;
    private static bool stopQueued = false;
    private static int hitPopups = 0;
    private static int missPopups = 0;
    private static bool replayViewerSeekHovered = false;
    private static bool leftMouseButtonDown = false;

    private static Sprite3D pauseHud;
    private static PauseHud pauseHudControl;
    private static float pauseState = 0;
    private static double pauseMs = 0;
    private static float pauseCooldown = 0;
    private static float pauseHoldTime = 0;
    private static bool spaceHeld = false;
    private static bool musicStarted = false;
    private static float pauseHoldDuration = 0.75f;
    private static float pauseRewindMs = 750f;
    private static float pauseMinimumProgressMs = 1000f;
    private static float pauseRampStartDb = -30f;
    private static float pauseRampRateDbPerSecond = 30f;
    private static float pauseCooldownDuration = 1f;
    private static float pauseDesyncThresholdMs = 100f;
    private static float pauseUiOpacity = 0.75f;
    private static int pauseUsedCount = 0;
    private static Panel quitOverlay;
    private static ColorRect quitProgressBar;
    private static float quitHoldTime = 0;
    private static bool rKeyHeld = false;
    private static float quitHoldDuration = 0.55f;
    private static readonly Color pause_counter_color = Color.Color8(255, 255, 255);
    private static readonly Color quit_hold_fill_start_color = Color.Color8(138, 138, 138, 210);
    private static readonly Color quit_hold_fill_end_color = Color.Color8(255, 255, 255, 255);

    private double lastFrame = Time.GetTicksUsec();     // delta arg unreliable..
                                                        //private double lastSecond = Time.GetTicksUsec();    // better framerate calculation
    private List<Dictionary<string, object>> lastCursorPositions = [];  // trail
                                                                        //private int frameCount = 0;
    private float skipLabelAlpha = 0;
    private float targetSkipLabelAlpha = 0;

    public static bool Playing = false;
    public static ulong Started = 0;
    public static bool MenuShown = false;
    public static bool SettingsShown = false;
    public static bool ReplayViewerShown = false;
    public static int ToProcess = 0;
    public static List<Note> ProcessNotes = [];
    public static Attempt CurrentAttempt = new();
    public static double MapLength;
    //public static Tween JesusTween;
    public static MeshInstance3D[] Cursors;

    public struct Attempt
    {
        private SettingsProfile settings;
        private LegacyRunner runner;

        public string ID = "";
        public bool Stopped = false;
        public bool IsReplay = false;
        public Replay[] Replays;    // when reading replays
        public float LongestReplayLength = 0;
        public List<float[]> ReplayFrames = []; // when writing replays
        public List<float> ReplaySkips = [];
        public ulong LastReplayFrame = 0;
        public uint ReplayFrameCountOffset = 0;
        public uint ReplayAttemptStatusOffset = 0;
        public Godot.FileAccess ReplayFile;
        public double Progress = 0; // ms
        public Map Map = new();
        public double Speed = 1;
        public double StartFrom = 0;
        public ulong FirstNote = 0;
        public Dictionary<string, bool> Mods;
        public string[] Players = [];
        public bool Alive = true;
        public bool Skippable = false;
        public bool Qualifies = true;
        public uint Hits = 0;
        public float[] HitsInfo = [];
        public Color LastHitColour = SkinManager.Instance.Skin.NoteColors[^1];
        public uint Misses = 0;
        public double DeathTime = -1;
        public uint Sum = 0;
        public uint Combo = 0;
        public uint ComboMultiplier = 1;
        public uint ComboMultiplierProgress = 0;
        public uint ComboMultiplierIncrement = 0;
        public double ModsMultiplier = 1;
        public uint Score = 0;
        public uint PassedNotes = 0;
        public double Accuracy = 100;
        public double Health = 100;
        public double HealthStep = 15;
        public Vector2 CursorPosition = Vector2.Zero;
        public Vector2 RawCursorPosition = Vector2.Zero;
        public double DistanceMM = 0;

        public Attempt(Map map, double speed, double startFrom, Dictionary<string, bool> mods, string[] players = null, Replay[] replays = null)
        {
            settings = SettingsManager.Instance.Settings;

            ID = $"{map.Name}_{OS.GetUniqueId()}_{Time.GetDatetimeStringFromUnixTime((long)Time.GetUnixTimeFromSystem())}".Replace(":", "_");
            Replays = replays;
            IsReplay = Replays != null;
            Map = map;
            Speed = speed;
            StartFrom = startFrom;
            Players = players ?? [];
            Progress = Speed * -1000 - settings.ApproachTime.Value * 1000 + StartFrom;
            ComboMultiplierIncrement = Math.Max(2, (uint)Map.Notes.Length / 200);
            Mods = [];
            HitsInfo = IsReplay ? Replays[0].Notes : new float[Map.Notes.Length];

            foreach (KeyValuePair<string, bool> mod in mods)
            {
                Mods[mod.Key] = mod.Value;
            }

            if (StartFrom > 0)
            {
                Qualifies = false;

                foreach (Note note in Map.Notes)
                {
                    if (note.Millisecond < StartFrom)
                    {
                        FirstNote = (ulong)note.Index + 1;
                    }
                }
            }

            if (!IsReplay && settings.RecordReplays && !Map.Ephemeral)
            {
                ReplayFile = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/replays/{ID}.phxr", Godot.FileAccess.ModeFlags.Write);
                ReplayFile.StoreString("phxr"); // sig
                ReplayFile.Store8(1);   // replay file version

                string mapFileName = Map.FilePath.GetFile().GetBaseName();

                ReplayFile.StoreDouble(Speed);
                ReplayFile.StoreDouble(StartFrom);
                ReplayFile.StoreDouble(settings.ApproachRate);
                ReplayFile.StoreDouble(settings.ApproachDistance);
                ReplayFile.StoreDouble(settings.FadeIn);
                ReplayFile.Store8((byte)(settings.FadeOut > 0 ? 1 : 0));
                ReplayFile.Store8((byte)(settings.Pushback ? 1 : 0));
                ReplayFile.StoreDouble(settings.CameraParallax);
                ReplayFile.StoreDouble(settings.FoV.Value);
                ReplayFile.StoreDouble(settings.NoteSize);
                ReplayFile.StoreDouble(settings.Sensitivity);

                ReplayAttemptStatusOffset = (uint)ReplayFile.GetPosition();

                ReplayFile.Store8(0);   // reserve attempt status

                string modifiers = "";
                string player = "You";

                foreach (KeyValuePair<string, bool> mod in Mods)
                {
                    if (mod.Value)
                    {
                        modifiers += $"{mod.Key}_";
                    }
                }

                modifiers = modifiers.TrimSuffix("_");

                ReplayFile.Store32((uint)modifiers.Length);
                ReplayFile.StoreString(modifiers);
                ReplayFile.Store32((uint)mapFileName.Length);
                ReplayFile.StoreString(mapFileName);
                ReplayFile.Store64((ulong)Map.Notes.Length);
                ReplayFile.Store32((uint)player.Length);
                ReplayFile.StoreString(player);

                ReplayFrameCountOffset = (uint)ReplayFile.GetPosition();

                ReplayFile.Store64(0);  // reserve frame count
            }
            else if (IsReplay)
            {
                foreach (Replay replay in Replays)
                {
                    if (replay.Length > LongestReplayLength)
                    {
                        LongestReplayLength = replay.Length;
                    }
                }
            }

            foreach (KeyValuePair<string, bool> entry in Mods)
            {
                if (entry.Value)
                {
                    bool hasMultiplier = Constants.MODS_MULTIPLIER_INCREMENT.TryGetValue(entry.Key, out double multiplier);
                    ModsMultiplier += hasMultiplier ? multiplier : 0;
                }
            }
        }

        public void Hit(int index)
        {
            Hits++;
            Sum++;
            Accuracy = Math.Floor((float)Hits / Sum * 10000) / 100;
            Combo++;
            ComboMultiplierProgress++;

            LastHitColour = SkinManager.Instance.Skin.NoteColors[index % SkinManager.Instance.Skin.NoteColors.Length];

            float lateness = IsReplay ? HitsInfo[index] : (float)(((int)Progress - Map.Notes[index].Millisecond) / Speed);
            float factor = 1 - Math.Max(0, lateness - 25) / 150f;

            if (!IsReplay)
            {
                Stats.NotesHit++;

                if (Combo > Stats.HighestCombo)
                {
                    Stats.HighestCombo = Combo;
                }

                HitsInfo[index] = lateness;
            }

            if (ComboMultiplierProgress == ComboMultiplierIncrement)
            {
                if (ComboMultiplier < 8)
                {
                    ComboMultiplierProgress = ComboMultiplier == 7 ? ComboMultiplierIncrement : 0;
                    ComboMultiplier++;

                    if (ComboMultiplier == 8)
                    {
                        multiplierColour = Color.Color8(255, 140, 0);
                    }
                }
            }

            uint hitScore = (uint)(100 * ComboMultiplier * ModsMultiplier * factor * ((Speed - 1) / 2.5 + 1));

            Score += hitScore;
            HealthStep = Math.Max(HealthStep / 1.45, 15);
            Health = Math.Min(100, Health + HealthStep / 1.75);
            Map.Notes[index].Hit = true;

            scoreLabel.Text = Util.String.PadMagnitude(Score.ToString());
            multiplierLabel.Text = $"{ComboMultiplier}x";
            hitsLabel.Text = $"{Hits}";
            hitsLabel.LabelSettings.FontColor = Color.Color8(255, 255, 255, 255);
            sumLabel.Text = Util.String.PadMagnitude(Sum.ToString());
            accuracyLabel.Text = $"{(Hits + Misses == 0 ? "100.00" : Accuracy.ToString("F2"))}%";
            comboLabel.Text = Combo.ToString();

            if (!settings.AlwaysPlayHitSound.Value)
            {
                SoundManager.PlayHitSound();
            }

            hitTween?.Kill();
            hitTween = hitsLabel.CreateTween();
            hitTween.TweenProperty(hitsLabel.LabelSettings, "font_color", Color.Color8(255, 255, 255, 160), 1);
            hitTween.Play();

            if (!settings.HitPopups || hitPopups >= 64)
            {
                return;
            }

            hitPopups++;

            Label3D popup = hit_feedback.Instantiate<Label3D>();
            node.AddChild(popup);
            popup.GlobalPosition = new Vector3(Map.Notes[index].X, -1.4f, 0);
            popup.Text = hitScore.ToString();

            Tween tween = popup.CreateTween();
            tween.TweenProperty(popup, "transparency", 1, 0.25f);
            tween.Parallel().TweenProperty(popup, "position", popup.Position + Vector3.Up / 4f, 0.25f).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(() =>
            {
                hitPopups--;
                popup.QueueFree();
            }));
            tween.Play();
        }

        public void Miss(int index)
        {
            Misses++;
            Sum++;
            Accuracy = Mathf.Floor((float)Hits / Sum * 10000) / 100;
            Combo = 0;
            ComboMultiplierProgress = 0;
            ComboMultiplier = Math.Max(1, ComboMultiplier - 1);
            Health = Math.Max(0, Health - HealthStep);
            HealthStep = Math.Min(HealthStep * 1.2, 100);

            if (!IsReplay)
            {
                HitsInfo[index] = -1;
                Stats.NotesMissed++;
            }

            //if (Health - HealthStep <= 0)
            //{
            //    Bell.Play();
            //    Jesus.Modulate = Color.Color8(255, 255, 255, 196);
            //
            //    JesusTween?.Kill();
            //    JesusTween = Jesus.CreateTween();
            //    JesusTween.TweenProperty(Jesus, "modulate", Color.Color8(255, 255, 255, 0), 1);
            //    JesusTween.Play();
            //}

            if (!IsReplay && Health <= 0)
            {
                if (Alive)
                {
                    Alive = false;
                    Qualifies = false;
                    DeathTime = Progress;
                    SoundManager.FailSound.Play();

                    healthTexture.Modulate = Color.Color8(255, 255, 255, 128);
                    healthTexture.GetParent().GetNode<TextureRect>("Background").Modulate = healthTexture.Modulate;
                }

                if (!Mods["NoFail"])
                {
                    QueueStop();
                }
            }

            SoundManager.PlayMissSound();

            multiplierLabel.Text = $"{ComboMultiplier}x";
            missesLabel.Text = $"{Misses}";
            simpleMissesLabel.Text = $"{Misses}";
            missesLabel.LabelSettings.FontColor = Color.Color8(255, 255, 255, 255);
            sumLabel.Text = Util.String.PadMagnitude(Sum.ToString());
            accuracyLabel.Text = $"{(Hits + Misses == 0 ? "100.00" : Accuracy.ToString("F2"))}%";
            comboLabel.Text = Combo.ToString();

            missTween?.Kill();
            missTween = missesLabel.CreateTween();
            missTween.TweenProperty(missesLabel.LabelSettings, "font_color", Color.Color8(255, 255, 255, 160), 1);
            missTween.Play();

            if (!settings.MissPopups || missPopups >= 64)
            {
                return;
            }

            missPopups++;

            Sprite3D icon = miss_feedback.Instantiate<Sprite3D>();
            node.AddChild(icon);
            icon.GlobalPosition = new Vector3(Map.Notes[index].X, -1.4f, 0);
            icon.Texture = SkinManager.Instance.Skin.MissFeedbackImage;

            Tween tween = icon.CreateTween();
            tween.TweenProperty(icon, "transparency", 1, 0.25f);
            tween.Parallel().TweenProperty(icon, "position", icon.Position + Vector3.Up / 4f, 0.25f).SetTrans(Tween.TransitionType.Quint).SetEase(Tween.EaseType.Out);
            tween.TweenCallback(Callable.From(() =>
            {
                missPopups--;
                icon.QueueFree();
            }));
            tween.Play();
        }

        public void Stop()
        {
            if (Stopped)
            {
                return;
            }

            Stopped = true;

            if (!IsReplay && ReplayFile != null)
            {
                ReplayFile.Seek(ReplayAttemptStatusOffset);
                ReplayFile.Store8((byte)(Alive ? (Qualifies ? 0 : 1) : 2));

                ReplayFile.Seek(ReplayFrameCountOffset);
                ReplayFile.Store64((ulong)ReplayFrames.Count);

                foreach (float[] frame in ReplayFrames)
                {
                    ReplayFile.StoreFloat(frame[0]);
                    ReplayFile.StoreFloat(frame[1]);
                    ReplayFile.StoreFloat(frame[2]);
                }

                ReplayFile.Seek(ReplayFile.GetLength());
                ReplayFile.Store64(FirstNote);
                ReplayFile.Store64(Sum);

                for (ulong i = FirstNote; i < FirstNote + Sum; i++)
                {
                    ReplayFile.Store8((byte)(HitsInfo[i] == -1 ? 255 : Math.Min(254, HitsInfo[i] * (254 / 55))));
                }

                ReplayFile.Store64((ulong)ReplaySkips.Count);

                foreach (float skip in ReplaySkips)
                {
                    ReplayFile.StoreFloat(skip);
                }

                ReplayFile.Close();
                ReplayFile = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/replays/{ID}.phxr", Godot.FileAccess.ModeFlags.ReadWrite);

                ulong length = ReplayFile.GetLength();
                byte[] hash = SHA256.HashData(ReplayFile.GetBuffer((long)length));

                ReplayFile.StoreBuffer(hash);
                ReplayFile.Close();

                CurrentAttempt.HitsInfo = CurrentAttempt.HitsInfo[0..(int)PassedNotes];
            }
            else if (IsReplay)
            {
                CurrentAttempt.HitsInfo = CurrentAttempt.HitsInfo[0..(int)CurrentAttempt.Replays[0].LastNote];
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();

        settings = SettingsManager.Instance.Settings;

        node = this;

        menu = GetNode<Panel>("Menu");
        //fpsCounter = GetNode<Label>("FPSCounter");

        // SubViewport holder = GetNode("SubViewportContainer").GetNode<SubViewport>("SubViewport");
        Node holder = this;

        Camera = holder.GetNode<Camera3D>("Camera3D");
        titleLabel = holder.GetNode<Label3D>("Title");
        comboLabel = holder.GetNode<Label3D>("Combo");
        speedLabel = holder.GetNode<Label3D>("Speed");
        skipLabel = holder.GetNode<Label3D>("Skip");
        progressLabel = holder.GetNode<Label3D>("Progress");
        cursor = holder.GetNode<MeshInstance3D>("Cursor");
        grid = holder.GetNode<MeshInstance3D>("Grid");
        videoQuad = holder.GetNode<MeshInstance3D>("Video");
        notesMultimesh = holder.GetNode<MultiMeshInstance3D>("Notes");
        cursorTrailMultimesh = holder.GetNode<MultiMeshInstance3D>("CursorTrail");
        //jesus = GetNode<TextureRect>("Jesus");

        healthPanel = holder.GetNode("Health");
        healthTexture = healthPanel.GetNode("HealthViewport").GetNode<TextureRect>("Main");
        progressBarNode = holder.GetNode("ProgressBar");
        progressBarTexture = progressBarNode.GetNode("ProgressBarViewport").GetNode<TextureRect>("Main");
        panelLeft = holder.GetNode("PanelLeft").GetNode<SubViewport>("PanelLeftViewport");
        panelRight = holder.GetNode("PanelRight").GetNode<SubViewport>("PanelRightViewport");
        //bell = GetNode<AudioStreamPlayer>("Bell");
        replayViewer = GetNode<Panel>("ReplayViewer");
        replayViewerPause = replayViewer.GetNode<TextureButton>("Pause");
        replayViewerLabel = replayViewer.GetNode<Label>("Time");
        replayViewerSeek = replayViewer.GetNode<HSlider>("Seek");
        accuracyLabel = panelRight.GetNode<Label>("Accuracy");
        hitsLabel = panelRight.GetNode<Label>("Hits");
        missesLabel = panelRight.GetNode<Label>("Misses");
        sumLabel = panelRight.GetNode<Label>("Sum");
        simpleMissesLabel = panelRight.GetNode<Label>("SimpleMisses");
        scoreLabel = panelLeft.GetNode<Label>("Score");
        multiplierLabel = panelLeft.GetNode<Label>("Multiplier");
        pauseCountLabel = panelLeft.GetNode<Label>("PauseCount");
        multiplierProgressPanel = panelLeft.GetNode<Panel>("MultiplierProgress");
        multiplierProgressMaterial = multiplierProgressPanel.Material as ShaderMaterial;
        video = videoQuad.GetNode("VideoViewport").GetNode<VideoStreamPlayer>("VideoStreamPlayer");

        List<string> activeMods = [];

        foreach (KeyValuePair<string, bool> mod in CurrentAttempt.Mods)
        {
            if (mod.Value)
            {
                activeMods.Add(mod.Key);
            }
        }

        for (int i = 0; i < activeMods.Count; i++)
        {
            if (activeMods[i] == "Spin")
            {
                continue;
            }

            Sprite3D icon = modifier_icon.Instantiate<Sprite3D>();

            AddChild(icon);

            icon.Position = new(i * 1.5f - (activeMods.Count - 1) / 1.5f, -8.5f, -10f);
            icon.Texture = Util.Misc.GetModIcon(activeMods[i]);
        }

        pauseHud = GetNode<Sprite3D>("PauseHud");
        pauseHudControl = pauseHud.GetNode<PauseHud>("PauseVP/Control");
        quitOverlay = GetNode<Panel>("QuitOverlay");
        quitProgressBar = quitOverlay.GetNode("Holder").GetNode("ProgressBackground").GetNode<ColorRect>("ProgressBar");

        pauseState = 0;
        pauseMs = 0;
        pauseCooldown = 0;
        pauseHoldTime = 0;
        spaceHeld = false;
        musicStarted = false;
        pauseUsedCount = 0;
        quitHoldTime = 0;
        rKeyHeld = false;
        updatePauseCounterVisuals();
        updatePauseHudVisualState();

        Panel menuButtonsHolder = menu.GetNode<Panel>("Holder");

        menu.GetNode<Button>("Button").Pressed += HideMenu;
        menuButtonsHolder.GetNode<Button>("Resume").Pressed += HideMenu;
        menuButtonsHolder.GetNode<Button>("Restart").Pressed += Restart;
        menuButtonsHolder.GetNode<Button>("Settings").Pressed += () =>
        {
            SettingsMenu.Instance.ShowMenu();
        };
        menuButtonsHolder.GetNode<Button>("Quit").Pressed += () =>
        {
            if (CurrentAttempt.Alive)
            {
                SoundManager.FailSound.Play();
            }

            CurrentAttempt.Alive = false;
            CurrentAttempt.Qualifies = false;

            if (CurrentAttempt.DeathTime == -1)
            {
                CurrentAttempt.DeathTime = Math.Max(0, CurrentAttempt.Progress);
            }

            Stop();
        };

        replayViewerPause.Pressed += () =>
        {
            Playing = !Playing;
            SoundManager.Song.PitchScale = Playing ? (float)CurrentAttempt.Speed : 0.00000000000001f;   // ooohh my goood
            replayViewerPause.TextureNormal = GD.Load<Texture2D>(Playing ? "res://textures/pause.png" : "res://textures/play.png");
        };

        replayViewerSeek.ValueChanged += value =>
        {
            replayViewerLabel.Text = $"{Util.String.FormatTime(value * CurrentAttempt.LongestReplayLength / 1000)} / {Util.String.FormatTime(CurrentAttempt.LongestReplayLength / 1000)}";
        };
        replayViewerSeek.DragEnded += _ =>
        {
            CurrentAttempt.Hits = 0;
            CurrentAttempt.Misses = 0;
            CurrentAttempt.Sum = 0;
            CurrentAttempt.Accuracy = 100;
            CurrentAttempt.Score = 0;
            CurrentAttempt.PassedNotes = 0;
            CurrentAttempt.Combo = 0;
            CurrentAttempt.ComboMultiplier = 1;
            CurrentAttempt.ComboMultiplierProgress = 0;
            CurrentAttempt.Health = 100;
            CurrentAttempt.HealthStep = 15;

            hitsLabel.Text = "0";
            missesLabel.Text = "0";
            simpleMissesLabel.Text = "0";
            sumLabel.Text = "0";
            accuracyLabel.Text = "100.00%";
            scoreLabel.Text = "0";
            comboLabel.Text = "0";
            multiplierLabel.Text = "1x";
            multiplierProgress = 0;
            multiplierColour = Color.Color8(255, 255, 255);

            for (int i = 0; i < CurrentAttempt.Map.Notes.Length; i++)
            {
                CurrentAttempt.Map.Notes[i].Hit = false;
            }

            CurrentAttempt.Progress = (float)replayViewerSeek.Value * CurrentAttempt.LongestReplayLength;

            for (int i = 0; i < CurrentAttempt.Replays.Length; i++)
            {
                Cursors[i].Transparency = 0;
                CurrentAttempt.Replays[i].Complete = false;

                for (int j = 0; j < CurrentAttempt.Replays[i].Frames.Length; j++)
                {
                    if (CurrentAttempt.Progress < CurrentAttempt.Replays[i].Frames[j].Progress)
                    {
                        CurrentAttempt.Replays[i].FrameIndex = Math.Max(0, j - 1);
                        break;
                    }
                }
            }

            if (!SoundManager.Song.Playing)
            {
                SoundManager.Song.Play();
            }

            double audioTime = Math.Max(0, CurrentAttempt.Progress + settings.LocalOffset.Value);
            SoundManager.Song.Seek((float)audioTime / 1000);
        };
        replayViewerSeek.FocusEntered += () =>
        {
            replayViewerSeekHovered = true;
        };
        replayViewerSeek.FocusExited += () =>
        {
            replayViewerSeekHovered = false;
        };

        if (settings.SimpleHUD.Value)
        {
            Godot.Collections.Array<Node> widgets = panelLeft.GetChildren();
            widgets.AddRange(panelRight.GetChildren());

            foreach (Node widget in widgets)
            {
                (widget as CanvasItem).Visible = false;
            }

            simpleMissesLabel.Visible = true;
        }

        bool superSimpleHUD = settings.SuperSimpleHUD.Value;
        if (superSimpleHUD)
        {
            // Apply everything SimpleHUD hides
            Godot.Collections.Array<Node> widgets = panelLeft.GetChildren();
            widgets.AddRange(panelRight.GetChildren());
            foreach (Node widget in widgets)
                (widget as CanvasItem).Visible = false;
            simpleMissesLabel.Visible = true;

            // Additionally hide title, progress text, combo, health, and progress bar
            titleLabel.Visible = false;
            progressLabel.Visible = false;
            comboLabel.Visible = false;

            if (healthPanel is CanvasItem healthCanvas)
                healthCanvas.Visible = false;
            else if (healthPanel is Node3D healthNode3D)
                healthNode3D.Visible = false;

            if (progressBarNode is CanvasItem progressBarCanvas)
                progressBarCanvas.Visible = false;
            else if (progressBarNode is Node3D progressBarNode3D)
                progressBarNode3D.Visible = false;
        }

        float fov = (float)(CurrentAttempt.IsReplay ? CurrentAttempt.Replays[0].FoV : settings.FoV.Value);

        MenuShown = false;
        Camera.Fov = fov;
        videoQuad.Transparency = 1;
        titleLabel.Text = CurrentAttempt.Map.PrettyTitle;
        hitsLabel.LabelSettings.FontColor = Color.Color8(255, 255, 255, 160);
        missesLabel.LabelSettings.FontColor = Color.Color8(255, 255, 255, 160);
        speedLabel.Text = $"{CurrentAttempt.Speed:F2}x";
        speedLabel.Modulate = Color.Color8(255, 255, 255, (byte)(CurrentAttempt.Speed == 1 ? 0 : 100));

        float videoHeight = 2 * (float)Math.Sqrt(Math.Pow(103.75 / Math.Cos(Mathf.DegToRad(fov / 2)), 2) - Math.Pow(103.75, 2));

        (videoQuad.Mesh as QuadMesh).Size = new(videoHeight / 0.5625f, videoHeight);    // don't use 16:9? too bad lol
        video.GetParent<SubViewport>().Size = new((int)(1920 * settings.VideoRenderScale.Value / 100), (int)(1080 * settings.VideoRenderScale.Value / 100));

        multiplierProgress = 0;
        multiplierColour = Color.Color8(255, 255, 255);

        multiplierProgressMaterial.SetShaderParameter("progress", 0);
        multiplierProgressMaterial.SetShaderParameter("colour", multiplierColour);
        multiplierProgressMaterial.SetShaderParameter("sides", Math.Clamp(CurrentAttempt.ComboMultiplierIncrement, 3, 32));

        Discord.Client.UpdateDetails("Playing a Map");
        Discord.Client.UpdateState(CurrentAttempt.Map.PrettyTitle);
        // Discord.Client.UpdateEndTime(DateTime.UtcNow.AddSeconds((Time.GetUnixTimeFromSystem() + CurrentAttempt.Map.Length / 1000 / CurrentAttempt.Speed)));

        Input.MouseMode = settings.AbsoluteInput.Value || CurrentAttempt.IsReplay ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured;
        Input.UseAccumulatedInput = false;

        (cursor.Mesh as QuadMesh).Size = new Vector2((float)(Constants.CURSOR_SIZE * settings.CursorScale.Value), (float)(Constants.CURSOR_SIZE * settings.CursorScale.Value));

        try
        {
            StandardMaterial3D cursorMaterial = cursor.MaterialOverride as StandardMaterial3D ?? cursor.GetActiveMaterial(0) as StandardMaterial3D;
            float cursorOpacity = Math.Clamp((float)settings.CursorOpacity.Value / 100, 0, 1);
            float cursorTransparency = 1f - cursorOpacity;

            cursor.Transparency = cursorTransparency;
            cursorMaterial?.AlbedoTexture = SkinManager.Instance.Skin.CursorImage;

            (cursorTrailMultimesh.MaterialOverride as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.CursorImage;
            (grid.GetActiveMaterial(0) as StandardMaterial3D).AlbedoTexture = SkinManager.Instance.Skin.GridImage;
            panelLeft.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelLeftBackgroundImage;
            panelRight.GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.PanelRightBackgroundImage;
            healthTexture.Texture = SkinManager.Instance.Skin.HealthImage;
            healthTexture.GetParent().GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.HealthBackgroundImage;
            progressBarTexture.Texture = SkinManager.Instance.Skin.ProgressImage;
            progressBarTexture.GetParent().GetNode<TextureRect>("Background").Texture = SkinManager.Instance.Skin.ProgressBackgroundImage;
            panelRight.GetNode<TextureRect>("HitsIcon").Texture = SkinManager.Instance.Skin.HitsImage;
            panelRight.GetNode<TextureRect>("MissesIcon").Texture = SkinManager.Instance.Skin.MissesImage;
            notesMultimesh.Multimesh.Mesh = SkinManager.Instance.Skin.NoteMesh;
        }
        catch (Exception exception)
        {
            ToastNotification.Notify("Could not load skin", 2);
            throw Logger.Error($"Could not load skin; {exception.Message}");
        }

        //string space = settings.Space == "skin" ? SkinProfile.GameSpace : settings.Space;
        //
        //if (space != "void")
        //{
        //    node3D.AddChild(GD.Load<PackedScene>($"res://prefabs/spaces/{space}.tscn").Instantiate<Node3D>());
        //}

        if (CurrentAttempt.Map.AudioBuffer != null)
        {
            SoundManager.Song.Stream = Util.Audio.LoadStream(CurrentAttempt.Map.AudioBuffer);
            SoundManager.Song.PitchScale = (float)CurrentAttempt.Speed;
            SoundManager.Song.VolumeDb = getTargetMusicVolumeDb();
            SoundManager.Song.Stop();
        }

        musicStarted = false;

        if (CurrentAttempt.Map.Notes != null && CurrentAttempt.Map.Notes.Length > 0)
        {
            MapLength = CurrentAttempt.Map.Notes[^1].Millisecond;
        }
        else if (CurrentAttempt.Map.AudioBuffer != null)
        {
            MapLength = (float)SoundManager.Song.Stream.GetLength() * 1000;
        }
        else
        {
            MapLength = CurrentAttempt.Map.Length + 1000;
        }

        MapLength += Constants.HIT_WINDOW + 1000;

        // TODO: Fix videos

        //if (settings.VideoDim < 100 && CurrentAttempt.Map.VideoBuffer != null)
        //{
        //    if (CurrentAttempt.Speed != 1)
        //    {
        //        ToastNotification.Notify("Videos currently only sync on 1x", 1);
        //    }
        //          else
        //          {
        //              //File.WriteAllBytes($"{Constants.USER_FOLDER}/cache/video.mp4", CurrentAttempt.Map.VideoBuffer);
        //              video.Stream.File = $"{MapUtil.MapsCacheFolder}/{CurrentAttempt.Map.Name}/video.mp4";
        //    }
        //}
        if (CurrentAttempt.Replays != null)
        {
            if (CurrentAttempt.Replays.Length > 1)
            {
                CurrentAttempt.Replays[0].Pushback = false;
            }

            CurrentAttempt.Mods["Spin"] = true;
            Cursors = new MeshInstance3D[CurrentAttempt.Replays.Length];

            for (int i = 0; i < CurrentAttempt.Replays.Length; i++)
            {
                if (!CurrentAttempt.Replays[i].Modifiers["Spin"])
                {
                    CurrentAttempt.Mods["Spin"] = false;
                }

                MeshInstance3D _cursor = cursor.Duplicate() as MeshInstance3D;
                _cursor.Name = $"_cursor{i}";
                node.AddChild(_cursor);
                Cursors[i] = _cursor;
            }

            cursor.Visible = false;
            ShowReplayViewer();
        }

        SoundManager.UpdateVolume();
    }

    public override void _PhysicsProcess(double delta)
    {
        multiplierProgress = Mathf.Lerp(multiplierProgress, (float)CurrentAttempt.ComboMultiplierProgress / CurrentAttempt.ComboMultiplierIncrement, Math.Min(1, (float)delta * 16));
        multiplierColour = multiplierColour.Lerp(Color.Color8(255, 255, 255), (float)delta * 2);
        multiplierProgressMaterial.SetShaderParameter("progress", multiplierProgress);

        if (multiplierColour.B < 255)   // fuck
        {
            multiplierProgressMaterial.SetShaderParameter("colour", multiplierColour);  // this loves causing lag spikes, keep track
        }
    }

    public override void _Process(double delta)
    {
        ulong now = Time.GetTicksUsec();
        delta = (now - lastFrame) / 1000000;    // more reliable
        lastFrame = now;
        pauseCooldown = Math.Max(0, pauseCooldown - (float)delta);
        updatePauseHudVisualState();
        skipLabelAlpha = Mathf.Lerp(skipLabelAlpha, targetSkipLabelAlpha, Math.Min(1, (float)delta * 20));

        if (rKeyHeld && !CurrentAttempt.IsReplay)
        {
            quitHoldTime += (float)delta;
            float progress = Math.Clamp(quitHoldTime / quitHoldDuration, 0, 1);
            quitProgressBar.Size = new Vector2(300 * progress, 8);
            quitProgressBar.Color = quit_hold_fill_start_color.Lerp(quit_hold_fill_end_color, progress);

            if (!quitOverlay.Visible)
            {
                quitOverlay.Visible = true;
                Tween showTween = quitOverlay.CreateTween();
                showTween.TweenProperty(quitOverlay, "modulate", Color.Color8(255, 255, 255, 255), 0.15);
                showTween.Play();
            }

            if (quitHoldTime >= quitHoldDuration)
            {
                rKeyHeld = false;

                if (CurrentAttempt.Alive)
                {
                    SoundManager.FailSound.Play();
                }

                CurrentAttempt.Alive = false;
                CurrentAttempt.Qualifies = false;

                if (CurrentAttempt.DeathTime == -1)
                {
                    CurrentAttempt.DeathTime = Math.Max(0, CurrentAttempt.Progress);
                }

                QueueStop();
                return;
            }
        }

        if (stopQueued)
        {
            stopQueued = false;
            Stop();
            return;
        }

        if (isPaused())
        {
            return;
        }

        if (isPauseRampActive())
        {
            updatePauseStateEachFrame(delta);
        }
        else
        {
            double audioDelay = CurrentAttempt.Progress - 1000 * (SoundManager.Song.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix());
        
            if (Math.Abs(audioDelay) > 25 && CurrentAttempt.Progress > 0)
            {
                SoundManager.Song.PitchScale = Math.Max(Mathf.Epsilon, (float)CurrentAttempt.Speed + (float)audioDelay / 1000);
            }
            else if (Math.Abs(SoundManager.Song.PitchScale - CurrentAttempt.Speed) > Mathf.Epsilon)
            {
                SoundManager.Song.PitchScale = (float)CurrentAttempt.Speed;
            }
        }

        if (!Playing || MenuShown)
        {
            return;
        }

        if (CurrentAttempt.IsReplay)
        {
            if (!replayViewerSeekHovered || !leftMouseButtonDown)
            {
                replayViewerSeek.Value = CurrentAttempt.Progress / CurrentAttempt.LongestReplayLength;
            }

            Vector2 positionSum = new();

            for (int i = 0; i < CurrentAttempt.Replays.Length; i++)
            {
                for (int j = CurrentAttempt.Replays[i].FrameIndex; j < CurrentAttempt.Replays[i].Frames.Length; j++)
                {
                    if (CurrentAttempt.Progress < CurrentAttempt.Replays[i].Frames[j].Progress)
                    {
                        CurrentAttempt.Replays[i].FrameIndex = Math.Max(0, j - 1);
                        break;
                    }
                }

                int next = Math.Min(CurrentAttempt.Replays[i].FrameIndex + 1, CurrentAttempt.Replays[i].Frames.Length - 2);

                if (!CurrentAttempt.Replays[i].Complete && CurrentAttempt.Progress >= CurrentAttempt.Replays[i].Length)
                {
                    CurrentAttempt.Replays[i].Complete = true;
                    CurrentAttempt.Replays[i].LastNote = CurrentAttempt.PassedNotes;

                    Tween tween = Cursors[i].CreateTween();
                    tween.TweenProperty(Cursors[i], "transparency", 1, 1).SetTrans(Tween.TransitionType.Quad);
                    tween.Play();
                }

                double inverse = Mathf.InverseLerp(CurrentAttempt.Replays[i].Frames[CurrentAttempt.Replays[i].FrameIndex].Progress, CurrentAttempt.Replays[i].Frames[next].Progress, CurrentAttempt.Progress);
                Vector2 cursorPos = CurrentAttempt.Replays[i].Frames[CurrentAttempt.Replays[i].FrameIndex].CursorPosition.Lerp(CurrentAttempt.Replays[i].Frames[next].CursorPosition, (float)Math.Clamp(inverse, 0, 1));

                try
                {
                    Cursors[i].Position = new(cursorPos.X, cursorPos.Y, 0);
                }
                catch { }   // dnc

                CurrentAttempt.Replays[i].CurrentPosition = cursorPos;
                positionSum += cursorPos;
            }

            Vector2 averagePosition = positionSum / CurrentAttempt.Replays.Length;
            Vector2 mouseDelta = averagePosition - CurrentAttempt.CursorPosition;

            if (CurrentAttempt.Mods["Spin"])
            {
                mouseDelta *= new Vector2(1, -1) / (float)CurrentAttempt.Replays[0].Sensitivity * 106;  // idk lol
            }

            UpdateCursor(mouseDelta);

            CurrentAttempt.CursorPosition = averagePosition;

            if (CurrentAttempt.Replays.Length == 1 && CurrentAttempt.Replays[0].SkipIndex < CurrentAttempt.Replays[0].Skips.Length && CurrentAttempt.Progress >= CurrentAttempt.Replays[0].Skips[CurrentAttempt.Replays[0].SkipIndex])
            {
                CurrentAttempt.Replays[0].SkipIndex++;
                Skip();
            }

            int complete = 0;

            foreach (Replay replay in CurrentAttempt.Replays)
            {
                if (replay.Complete)
                {
                    complete++;
                }
            }

            if (complete == CurrentAttempt.Replays.Length)
            {
                QueueStop();
            }
        }
        else if (!CurrentAttempt.Stopped && settings.RecordReplays && !CurrentAttempt.Map.Ephemeral && now - CurrentAttempt.LastReplayFrame >= 1000000 / 60)    // 60hz
        {
            if (CurrentAttempt.ReplayFrames.Count == 0 || (CurrentAttempt.ReplayFrames[^1][1..2] != new float[] { CurrentAttempt.CursorPosition.X, CurrentAttempt.CursorPosition.Y }))
            {
                CurrentAttempt.LastReplayFrame = now;
                CurrentAttempt.ReplayFrames.Add([
                    (float)CurrentAttempt.Progress,
                    CurrentAttempt.CursorPosition.X,
                    CurrentAttempt.CursorPosition.Y
                ]);
            }
        }

        CurrentAttempt.Progress += delta * 1000 * CurrentAttempt.Speed;
        CurrentAttempt.Skippable = false;

        startGameplayMediaAtExpected(isPauseRampActive() ? SoundManager.Song.VolumeDb : getTargetMusicVolumeDb());
            
        int nextNoteMillisecond = CurrentAttempt.PassedNotes >= CurrentAttempt.Map.Notes.Length ? (int)MapLength + 5000 : CurrentAttempt.Map.Notes[CurrentAttempt.PassedNotes].Millisecond;
        int lastNoteMillisecond = CurrentAttempt.PassedNotes > 0 ? CurrentAttempt.Map.Notes[CurrentAttempt.PassedNotes - 1].Millisecond : 0;

        if (nextNoteMillisecond - lastNoteMillisecond > 5000 && nextNoteMillisecond >= Math.Max(CurrentAttempt.Progress + 3000 * CurrentAttempt.Speed, 1100 * CurrentAttempt.Speed))
        {
            CurrentAttempt.Skippable = true;
        }

        ToProcess = 0;
        ProcessNotes.Clear();

        // note process check
        double at = CurrentAttempt.IsReplay ? CurrentAttempt.Replays[0].ApproachTime : settings.ApproachTime;

        for (uint i = CurrentAttempt.PassedNotes; i < CurrentAttempt.Map.Notes.Length; i++)
        {
            Note note = CurrentAttempt.Map.Notes[i];

            if (note.Millisecond < CurrentAttempt.StartFrom)
            {
                continue;
            }

            if (note.Millisecond + Constants.HIT_WINDOW * CurrentAttempt.Speed < CurrentAttempt.Progress)   // past hit window
            {
                if (i + 1 > CurrentAttempt.PassedNotes)
                {
                    if (CurrentAttempt.IsReplay && CurrentAttempt.Replays.Length <= 1 && CurrentAttempt.Replays[0].Notes[note.Index] == -1 || !CurrentAttempt.IsReplay && !note.Hit)
                    {
                        CurrentAttempt.Miss(note.Index);
                    }

                    CurrentAttempt.PassedNotes = i + 1;
                }

                if (!CurrentAttempt.IsReplay)
                {
                    continue;
                }
            }
            else if (note.Millisecond > CurrentAttempt.Progress + at * 1000 * CurrentAttempt.Speed) // past approach distance
            {
                break;
            }
            else if (note.Hit)  // no point
            {
                continue;
            }

            if (settings.AlwaysPlayHitSound && !CurrentAttempt.Map.Notes[i].Hittable && note.Millisecond < CurrentAttempt.Progress)
            {
                CurrentAttempt.Map.Notes[i].Hittable = true;

                SoundManager.PlayHitSound();
            }

            ToProcess++;
            ProcessNotes.Add(note);
        }

        // hitreg check
        for (int i = 0; i < ToProcess; i++)
        {
            Note note = ProcessNotes[i];

            if (note.Hit)
            {
                continue;
            }

            if (!CurrentAttempt.IsReplay)
            {
                if (note.Millisecond - CurrentAttempt.Progress > 0)
                {
                    continue;
                }
                else if (CurrentAttempt.CursorPosition.X + Constants.HIT_BOX_SIZE >= note.X - 0.5f && CurrentAttempt.CursorPosition.X - Constants.HIT_BOX_SIZE <= note.X + 0.5f && CurrentAttempt.CursorPosition.Y + Constants.HIT_BOX_SIZE >= note.Y - 0.5f && CurrentAttempt.CursorPosition.Y - Constants.HIT_BOX_SIZE <= note.Y + 0.5f)
                {
                    CurrentAttempt.Hit(note.Index);
                }
            }
            else if (CurrentAttempt.Replays.Length > 1 && note.Millisecond - CurrentAttempt.Progress <= 0 || CurrentAttempt.Replays[0].Notes[note.Index] != -1 && note.Millisecond - CurrentAttempt.Progress + CurrentAttempt.Replays[0].Notes[note.Index] * CurrentAttempt.Speed <= 0)
            {
                CurrentAttempt.Hit(note.Index);
            }
        }

        if (CurrentAttempt.Progress >= MapLength)
        {
            Stop();
            return;
        }

        if (CurrentAttempt.Skippable)
        {
            targetSkipLabelAlpha = 100f / 255f;
            progressLabel.Modulate = Color.Color8(255, 255, 255, (byte)(96 + (int)(140 * (Math.Sin(Math.PI * now / 750000) / 2 + 0.5))));
        }
        else
        {
            targetSkipLabelAlpha = 0;
            progressLabel.Modulate = Color.Color8(255, 255, 255, 190);
        }

        progressLabel.Text = $"{Util.String.FormatTime(Math.Max(0, CurrentAttempt.Progress) / 1000)} / {Util.String.FormatTime(MapLength / 1000)}";
        healthTexture.Size = healthTexture.Size.Lerp(new Vector2(32 + (float)CurrentAttempt.Health * 10.24f, 80), Math.Min(1, (float)delta * 64));

        Vector2 progressSize = new Vector2(32 + (float)(CurrentAttempt.Progress / MapLength) * 1024, 80);

        if ((int)progressSize.X != (int)progressBarTexture.Size.X)
        {
            progressBarTexture.Size = progressSize;
        }

        skipLabel.Modulate = Color.Color8(255, 255, 255, (byte)(skipLabelAlpha * 255));
        cursor.RotationDegrees += Vector3.Back * (float)settings.CursorRotation * (float)delta;

        // trail stuff
        if (settings.CursorTrail)
        {
            List<Dictionary<string, object>> culledList = [];

            lastCursorPositions.Add(new()
            {
                ["Time"] = now,
                ["Position"] = CurrentAttempt.CursorPosition,
                ["Rotation"] = cursor.Rotation.Z
            });

            foreach (Dictionary<string, object> entry in lastCursorPositions)
            {
                if (now - (ulong)entry["Time"] >= (settings.TrailTime * 1000000))
                {
                    continue;
                }

                if (CurrentAttempt.CursorPosition.DistanceTo((Vector2)entry["Position"]) == 0)
                {
                    continue;
                }

                culledList.Add(entry);
            }

            int count = culledList.Count;
            float size = ((Vector2)cursor.Mesh.Get("size")).X;
            Transform3D transform = new Transform3D(new Vector3(size, 0, 0), new Vector3(0, size, 0), new Vector3(0, 0, size), Vector3.Zero);
            int j = 0;

            cursorTrailMultimesh.Multimesh.InstanceCount = count;

            foreach (Dictionary<string, object> entry in culledList)
            {
                ulong difference = now - (ulong)entry["Time"];
                uint alpha = (uint)(difference / (settings.TrailTime * 1000000) * 255);

                transform.Origin = new Vector3(((Vector2)entry["Position"]).X, ((Vector2)entry["Position"]).Y, 0);
                transform = transform.RotatedLocal(Vector3.Back, (float)entry["Rotation"]);

                cursorTrailMultimesh.Multimesh.SetInstanceTransform(j, transform);
                cursorTrailMultimesh.Multimesh.SetInstanceColor(j, Color.FromHtml($"ffffff{255 - alpha:X2}"));
                j++;
            }
        }
        else
        {
            cursorTrailMultimesh.Multimesh.InstanceCount = 0;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (CurrentAttempt.Stopped) return;

        if (@event is InputEventMouseMotion eventMouseMotion && !MenuShown && (Playing || isPaused() || isPauseRampActive()) && !CurrentAttempt.IsReplay)
        {
            if (!settings.AbsoluteInput)
            {
                UpdateCursor(eventMouseMotion.Relative);
            }
            else
            {
                // Take mouse position difference between center of the current window size
                // This is to make the mouse position the same as relative if it was locked, or confined
                Vector2 AbsolutePosition = eventMouseMotion.Position - (GetViewport().GetWindow().Size / 2);

                // Multiply by 0.582f to make it 1:1 to absolute scale on nightly
                UpdateCursor(AbsolutePosition * 0.582f);
            }

            CurrentAttempt.DistanceMM += eventMouseMotion.Relative.Length() / settings.Sensitivity / 57.5;
        }
        else if (@event is InputEventKey eventKey)
        {
            Key key = eventKey.PhysicalKeycode != Key.None ? eventKey.PhysicalKeycode : eventKey.Keycode;

            if (key == Key.R)
            {
                if (eventKey.Pressed && !eventKey.Echo)
                {
                    rKeyHeld = true;
                    quitHoldTime = 0;
                }
                else if (!eventKey.Pressed)
                {
                    rKeyHeld = false;
                    quitHoldTime = 0;
                    hideQuitOverlay();
                }

                return;
            }

            if (key == Key.Space && !CurrentAttempt.IsReplay)
            {
                if (isPaused())
                {
                    if (eventKey.Pressed && !eventKey.Echo)
                    {
                        beginUnpause();
                    }
                    else if (!eventKey.Pressed)
                    {
                        cancelUnpause();
                    }

                    return;
                }

                if (isPauseRampActive())
                {
                    if (!eventKey.Pressed)
                    {
                        cancelUnpause();
                    }

                    return;
                }
            }

            if (eventKey.Pressed && !eventKey.Echo)
            {
                switch (key)
                {
                    case Key.Escape:
                        CurrentAttempt.Qualifies = false;

                        if (SettingsMenu.Instance.Shown)
                        {
                            SettingsMenu.Instance.HideMenu();
                        }
                        else
                        {
                            ShowMenu(!MenuShown);
                        }

                        break;
                    case Key.Quoteleft:
                        Restart();
                        break;
                    case Key.F1:
                        if (CurrentAttempt.IsReplay)
                        {
                            ShowReplayViewer(!ReplayViewerShown);
                        }
                        break;
                    case Key.Space:
                        if (CurrentAttempt.IsReplay)
                        {
                            Playing = !Playing;
                            SoundManager.Song.PitchScale = Playing ? (float)CurrentAttempt.Speed : 0.00000000000001f;   // ooohh my goood
                            replayViewerPause.TextureNormal = GD.Load<Texture2D>(Playing ? "res://textures/ui/pause.png" : "res://textures/ui/play.png");
                        }
                        else
                        {
                            if (Lobby.Players.Count > 1) break;
                            if (CurrentAttempt.Skippable) Skip();
                            else if (settings.SpaceToPause) beginPause();
                        }
                        break;
                    case Key.F:
                        settings.FadeOut.Value = settings.FadeOut.Value > 0 ? 0 : 100;
                        break;
                    case Key.P:
                        settings.Pushback.Value = !settings.Pushback;
                        break;
                }
            }
        }
        else if (@event is InputEventMouseButton eventMouseButton)
        {
            switch (eventMouseButton.ButtonIndex)
            {
                case MouseButton.Left:
                    leftMouseButtonDown = eventMouseButton.Pressed;
                    break;
            }
        }
    }

    public override void Load()
    {
        base.Load();

        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

        MenuCursor.Instance.UpdateVisible(false, false);
        SceneManager.Space.UpdateState(true);
        SceneManager.Space.UpdateMap(CurrentAttempt.Map);
    }

    public static void Play(Map map, double speed = 1, double startFrom = 0, Dictionary<string, bool> mods = null, string[] players = null, Replay[] replays = null)
    {
        map = MapParser.Decode(map.FilePath);

        SceneManager.Root.GetViewport().GuiGetFocusOwner()?.ReleaseFocus();

        SoundManager.MenuMusic?.Stop();

        if (Playing)
        {
            Stop();
        }

        CurrentAttempt = new(map, speed, startFrom, mods ?? [], players, replays);
        SoundManager.BeginGameplayScope(CurrentAttempt.Map);
        Playing = true;
        stopQueued = false;
        Started = Time.GetTicksUsec();
        ProcessNotes = [];

        if (!CurrentAttempt.IsReplay)
        {
            Stats.Attempts++;

            if (!Stats.FavoriteMaps.ContainsKey(map.Name))
            {
                Stats.FavoriteMaps[map.Name] = 1;
            }
            else
            {
                Stats.FavoriteMaps[map.Name]++;
            }
        }

        SceneManager.Load("res://scenes/game.tscn");
    }

    public static void Restart()
    {
        CurrentAttempt.Alive = false;
        CurrentAttempt.Qualifies = false;
        Stop(false);

        SceneManager.ReloadCurrentScene();
        Play(MapParser.Decode(CurrentAttempt.Map.FilePath), CurrentAttempt.Speed, CurrentAttempt.StartFrom, CurrentAttempt.Mods, CurrentAttempt.Players, CurrentAttempt.Replays);
    }

    public static void Skip()
    {
        if (CurrentAttempt.Skippable)
        {
            CurrentAttempt.ReplaySkips.Add((float)CurrentAttempt.Progress);

            if (CurrentAttempt.PassedNotes >= CurrentAttempt.Map.Notes.Length)
            {
                CurrentAttempt.Progress = SoundManager.Song.Stream.GetLength() * 1000;
            }
            else
            {
                CurrentAttempt.Progress = CurrentAttempt.Map.Notes[CurrentAttempt.PassedNotes].Millisecond - settings.ApproachTime * 1500 * CurrentAttempt.Speed; // turn AT to ms and multiply by 1.5x

                // Discord.Client.UpdateEndTime(DateTime.UtcNow.AddSeconds((Time.GetUnixTimeFromSystem() + (CurrentAttempt.Map.Length - CurrentAttempt.Progress) / 1000 / CurrentAttempt.Speed)));

                if (CurrentAttempt.Map.AudioBuffer != null)
                {
                    if (!SoundManager.Song.Playing)
                    {
                        SoundManager.Song.Play();
                    }

                    double targetTime = Math.Max(0, CurrentAttempt.Progress + settings.LocalOffset.Value);
                    SoundManager.Song.Seek((float)targetTime / 1000);
                    video.StreamPosition = (float)targetTime / 1000;
                }
            }
        }
    }

    public static void QueueStop()
    {
        if (CurrentAttempt.Stopped || stopQueued)
        {
            return;
        }

        Playing = false;
        stopQueued = true;
    }

    public static void Stop(bool results = true)
    {
        if (CurrentAttempt.Stopped)
        {
            return;
        }

        CurrentAttempt.Stop();

        if (!CurrentAttempt.IsReplay)
        {
            Stats.GamePlaytime += (Time.GetTicksUsec() - Started) / 1000000;
            Stats.TotalDistance += (ulong)CurrentAttempt.DistanceMM;

            if (CurrentAttempt.StartFrom == 0)
            {
                if (!File.Exists($"{Constants.USER_FOLDER}/pbs/{CurrentAttempt.Map.Name}"))
                {
                    List<byte> bytes = [0, 0, 0, 0];
                    bytes.AddRange(SHA256.HashData([0, 0, 0, 0]));
                    File.WriteAllBytes($"{Constants.USER_FOLDER}/pbs/{CurrentAttempt.Map.Name}", [.. bytes]);
                }

                Leaderboard leaderboard = new(CurrentAttempt.Map.Name, $"{Constants.USER_FOLDER}/pbs/{CurrentAttempt.Map.Name}");

                leaderboard.Add(new(CurrentAttempt.ID, "You", CurrentAttempt.Qualifies, CurrentAttempt.Score, CurrentAttempt.Accuracy, Time.GetUnixTimeFromSystem(), CurrentAttempt.Progress, CurrentAttempt.Map.Length, CurrentAttempt.Speed, CurrentAttempt.Mods));
                leaderboard.Save();

                if (CurrentAttempt.Qualifies)
                {
                    Stats.Passes++;
                    Stats.TotalScore += CurrentAttempt.Score;

                    if (CurrentAttempt.Accuracy == 100)
                    {
                        Stats.FullCombos++;
                    }

                    if (CurrentAttempt.Score > Stats.HighestScore)
                    {
                        Stats.HighestScore = CurrentAttempt.Score;
                    }

                    Stats.PassAccuracies.Add(CurrentAttempt.Accuracy);
                }
            }
        }

        if (results)
        {
            SceneManager.Load("res://scenes/results.tscn");
        }
    }

    private static bool isPaused() => pauseState < 0;

    private static bool isPauseRampActive() => pauseState > 0;

    private static float getTargetMusicVolumeDb()
    {
        return SoundManager.ComputeVolumeDb((float)settings.VolumeMusic.Value, (float)settings.VolumeMaster.Value, 70);
    }

    private static double getExpectedAudioTimeMs(bool includeLocalOffset = true)
    {
        return CurrentAttempt.Progress + (includeLocalOffset ? settings.LocalOffset.Value : 0);
    }

    private static void stopGameplayMedia()
    {
        if (CurrentAttempt.Map.AudioBuffer != null && SoundManager.Song.Playing)
        {
            SoundManager.Song.Stop();
        }

        musicStarted = false;

        if (CurrentAttempt.Map.VideoBuffer != null && video.IsPlaying())
        {
            video.Stop();
        }
    }

    private static void startGameplayMediaAtExpected(float targetVolumeDb, bool includeLocalOffset = true)
    {
        double audioTime = getExpectedAudioTimeMs(includeLocalOffset);
        double videoTime = getExpectedAudioTimeMs(includeLocalOffset);

        if (CurrentAttempt.Map.AudioBuffer != null && audioTime >= 0 && CurrentAttempt.Progress < MapLength)
        {
            SoundManager.Song.VolumeDb = targetVolumeDb;

            if (!musicStarted || !SoundManager.Song.Playing)
            {
                SoundManager.Song.Play();
                SoundManager.Song.Seek((float)audioTime / 1000);
                musicStarted = true;
            }
        }

        if (CurrentAttempt.Map.VideoBuffer != null && settings.VideoDim < 100 && videoTime >= 0)
        {
            if (!video.IsPlaying())
            {
                video.Play();
                video.StreamPosition = (float)videoTime / 1000;

                Tween videoInTween = videoQuad.CreateTween();
                videoInTween.TweenProperty(videoQuad, "transparency", settings.VideoDim / 100, 0.5);
                videoInTween.Play();
            }
        }
    }

    private static void beginPause()
    {
        if (CurrentAttempt.IsReplay || MenuShown || pauseCooldown > 0 || !settings.SpaceToPause || CurrentAttempt.Progress <= pauseMinimumProgressMs * CurrentAttempt.Speed || CurrentAttempt.Progress >= MapLength)
        {
            return;
        }

        pauseMs = CurrentAttempt.Progress;
        pauseState = -1;
        Playing = false;
        spaceHeld = false;
        pauseHoldTime = 0;
        pauseUsedCount++;
        CurrentAttempt.Qualifies = false;
        resetPauseUi();
        updatePauseCounterVisuals();
        updatePauseHudVisualState();
        stopGameplayMedia();
    }

    private static void beginUnpause()
    {
        if (!isPaused())
        {
            return;
        }

        spaceHeld = true;
        pauseHoldTime = 0;
        pauseState = 1f;
        CurrentAttempt.Progress = Math.Max(0, pauseMs - pauseRewindMs * CurrentAttempt.Speed);
        pauseHudControl.SetProgress(0);
        updatePauseHudVisualState();
        startGameplayMediaAtExpected(pauseRampStartDb, false);
        Playing = true;
    }

    private static void cancelUnpause()
    {
        if (!isPauseRampActive())
        {
            return;
        }

        pauseState = -1f;
        spaceHeld = false;
        pauseHoldTime = 0;
        CurrentAttempt.Progress = pauseMs;
        resetPauseUi();
        updatePauseHudVisualState();
        stopGameplayMedia();
        Playing = false;
    }

    private static void completeUnpause()
    {
        pauseState = 0f;
        spaceHeld = false;
        pauseHoldTime = 0;
        pauseCooldown = pauseCooldownDuration;
        pauseHudControl.SetProgress(1);
        SoundManager.Song.VolumeDb = getTargetMusicVolumeDb();
        Playing = true;
        updatePauseHudVisualState();
    }

    private static void updatePauseStateEachFrame(double delta)
    {
        if (!isPauseRampActive())
        {
            return;
        }

        if (!spaceHeld)
        {
            cancelUnpause();
            return;
        }

        pauseHoldTime += (float)delta;
        pauseState = Math.Max(0, pauseState - (float)(delta / pauseHoldDuration));
        pauseHudControl.SetProgress(Math.Clamp(1f - pauseState, 0f, 1f));
        
        if (CurrentAttempt.Map.AudioBuffer != null && musicStarted && SoundManager.Song.Playing)
        {
            SoundManager.Song.VolumeDb = Mathf.Lerp(getTargetMusicVolumeDb() - 60, getTargetMusicVolumeDb(), 1 - pauseState);
        }

        if (pauseState == 0)
        {
            completeUnpause();
        }
    }

    private static void resetPauseUi()
    {
        spaceHeld = false;
        pauseHoldTime = 0;
        pauseHudControl.SetProgress(0);
    }

    private static void updatePauseCounterVisuals()
    {
        pauseCountLabel.Text = pauseUsedCount.ToString();

        if (node?.GetTree() == null)
        {
            return;
        }

        bool showPauseCounter = settings.SpaceToPause.Value && pauseUsedCount > 0 && !settings.SimpleHUD.Value && !settings.SuperSimpleHUD.Value;

        foreach (Node groupNode in node.GetTree().GetNodesInGroup("pause_text"))
        {
            if (groupNode is CanvasItem canvasItem)
            {
                canvasItem.Visible = showPauseCounter;
                canvasItem.Modulate = pause_counter_color;
            }
        }
    }

    private static void updatePauseHudVisualState()
    {
        if (pauseHud == null || pauseHudControl == null)
        {
            return;
        }

        float maxPercent = pauseState == -1f ? 0f : 1f;
        float percent = Math.Clamp(1f - pauseState, 0f, maxPercent);
        bool hideOverlay = isPaused() && Input.IsPhysicalKeyPressed(Key.C);
        bool hasPausedAtLeastOnce = pauseUsedCount > 0;
        pauseHud.Visible = hasPausedAtLeastOnce && pauseState != 0f && !hideOverlay;
        pauseHud.Modulate = new Color(1, 1, 1, Mathf.Abs(pauseState) * pauseUiOpacity);
        pauseHudControl.SetProgress(percent);
    }

    public static void ShowPause(bool show = true)
    {
        if (show)
        {
            beginPause();
        }
        else if (isPauseRampActive())
        {
            cancelUnpause();
        }
        else if (isPaused())
        {
            beginUnpause();
        }
    }

    public static void HidePause()
    {
        cancelUnpause();
    }

    private static void hideQuitOverlay()
    {
        quitProgressBar.Size = new Vector2(0, 8);
        quitProgressBar.Color = quit_hold_fill_start_color;

        if (quitOverlay.Visible)
        {
            Tween tween = quitOverlay.CreateTween();
            tween.TweenProperty(quitOverlay, "modulate", Color.Color8(255, 255, 255, 0), 0.15);
            tween.TweenCallback(Callable.From(() => { quitOverlay.Visible = false; }));
            tween.Play();
        }
    }

    public static void ShowMenu(bool show = true)
    {
        if (isPauseRampActive())
        {
            return;
        }

        MenuShown = show;

        if (MenuShown)
        {
            stopGameplayMedia();
        }
        else if (!isPaused())
        {
            startGameplayMediaAtExpected(getTargetMusicVolumeDb());
        }

        MenuCursor.Instance.UpdateVisible(MenuShown && SettingsManager.Instance.Settings.UseCursorInMenus.Value);

        if (MenuShown)
        {
            menu.Visible = true;
            Input.WarpMouse(node.GetViewport().GetWindow().Size / 2);
        }
        else
        {
            Input.MouseMode = settings.AbsoluteInput || CurrentAttempt.IsReplay ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured;
        }

        Tween tween = menu.CreateTween();
        tween.TweenProperty(menu, "modulate", Color.Color8(255, 255, 255, (byte)(MenuShown ? 255 : 0)), 0.25).SetTrans(Tween.TransitionType.Quad);
        tween.TweenCallback(Callable.From(() =>
        {
            menu.Visible = MenuShown;
        }));
        tween.Play();
    }

    public static void HideMenu()
    {
        ShowMenu(false);
    }

    public static void ShowReplayViewer(bool show = true)
    {
        ReplayViewerShown = CurrentAttempt.IsReplay && show;
        replayViewer.Visible = ReplayViewerShown;

        Input.MouseMode = ReplayViewerShown ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Hidden;
    }

    public static void HideReplayViewer()
    {
        ShowReplayViewer(false);
    }

    public static void UpdateCursor(Vector2 mouseDelta)
    {
        float sensitivity = (float)(CurrentAttempt.IsReplay ? CurrentAttempt.Replays[0].Sensitivity : settings.Sensitivity);
        sensitivity *= (float)settings.FoV.Value / 70f;

        if (settings.AbsoluteInput)
        {
            // Reset everything to zero so it doesn't spin endlessly, or have infinite sensitivity
            Camera.Rotation = Vector3.Zero;
            CurrentAttempt.RawCursorPosition = Vector2.Zero;
            CurrentAttempt.CursorPosition = Vector2.Zero;
        }

        if (!CurrentAttempt.Mods["Spin"])
        {
            if (settings.CursorDrift)
            {
                CurrentAttempt.CursorPosition = (CurrentAttempt.CursorPosition + new Vector2(1, -1) * mouseDelta / 120 * sensitivity).Clamp(-Constants.BOUNDS, Constants.BOUNDS);
            }
            else
            {
                CurrentAttempt.RawCursorPosition += new Vector2(1, -1) * (mouseDelta * sensitivity / 120f);
                CurrentAttempt.CursorPosition = CurrentAttempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);
            }

            cursor.Position = new Vector3(CurrentAttempt.CursorPosition.X, CurrentAttempt.CursorPosition.Y, 0);
            Camera.Position = new Vector3(0, 0, 3.75f) + new Vector3(CurrentAttempt.CursorPosition.X, CurrentAttempt.CursorPosition.Y, 0) * (float)(CurrentAttempt.IsReplay ? CurrentAttempt.Replays[0].Parallax : settings.CameraParallax);
            Camera.Rotation = Vector3.Zero;

            videoQuad.Position = new Vector3(Camera.Position.X, Camera.Position.Y, -100);
        }
        else
        {
            Camera.Rotation += new Vector3(-mouseDelta.Y / 120 * sensitivity / (float)Math.PI, -mouseDelta.X / 120 * sensitivity / (float)Math.PI, 0);

            Camera.Rotation = new Vector3((float)Math.Clamp(Camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)), Camera.Rotation.Y, Camera.Rotation.Z);

            Vector3 Origin = new Vector3(0, 0, 3.5f);
            Vector3 CursorLock = new Vector3(CurrentAttempt.CursorPosition.X, CurrentAttempt.CursorPosition.Y, 0);
            // The pivot is to mimic ROBLOX's orbital camera
            Vector3 Pivot = Camera.Basis.Z / 4f;

            Camera.Position = Origin + CursorLock * (float)settings.CameraParallax + Pivot;

            Vector3 LookVector = Camera.Basis.Z;
            Vector2 CameraVec2 = new Vector2(Camera.Position.X, Camera.Position.Y);
            Vector2 LookVec2 = new Vector2(LookVector.X, LookVector.Y);

            CurrentAttempt.RawCursorPosition = CameraVec2 - LookVec2 * Mathf.Abs(Camera.Position.Z / LookVector.Z);

            CurrentAttempt.CursorPosition = CurrentAttempt.RawCursorPosition.Clamp(-Constants.BOUNDS, Constants.BOUNDS);
            cursor.Position = new Vector3(CurrentAttempt.CursorPosition.X, CurrentAttempt.CursorPosition.Y, 0);

            videoQuad.Position = Camera.Position - Camera.Basis.Z * 103.75f;
            videoQuad.Rotation = Camera.Rotation;
        }
    }

    public static void UpdateScore(string player, int score)
    {
        //ColorRect playerScore = Leaderboard.GetNode("SubViewport").GetNode("Players").GetNode<ColorRect>(player);
        //Label scoreLabel = playerScore.GetNode<Label>("Score");
        //playerScore.Position = new Vector2(playerScore.Position.X, score);
        //scoreLabel.Text = score.ToString();
    }

}
