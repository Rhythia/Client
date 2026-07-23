using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;

public partial class Runner : Node3D
{
    [Signal] public delegate void AttemptStatsUpdatedEventHandler(Attempt attempt);
    [Signal] public delegate void SkipAvailableEventHandler(Attempt attempt);
    [Signal] public delegate void HitResultChangedEventHandler(int noteIndex, HitResult hitResult);

    [Export] public HudManager HudManager;

    public Attempt Attempt;
    public Dictionary<Type, int> ObjectIndicesStart = [];
    public Dictionary<Type, int> ObjectIndicesEnd = [];

    public Map Map;
    public double Speed = 1;
    public bool Paused = false;
    public bool Playing = false;
    public bool StopQueued = false;

    private SettingsProfile settings;
    private double lastFrame = Time.GetTicksUsec();
    private bool firstFrame = true;
    private bool eventsConnected = false;

    [ExportCategory("Settings")]
    [Export] public bool NotesOnly = false;

    [ExportCategory("Nodes")]
    [Export] public Camera3D Camera;
    [Export] public Godot.Collections.Array<Renderer> Renderers;
    [Export] public MeshInstance3D Grid;
    [Export] public MeshInstance3D Cursor;
    [Export] public VideoStreamPlayer VideoStreamPlayer;

    public override void _Ready()
    {
        base._Ready();

        HudManager ??= GetNode<HudManager>("HUD");
        Camera ??= GetNode<Camera3D>("Camera3D");
        Grid ??= HudManager.GetNode<MeshInstance3D>("Grid");
        Cursor ??= GetNode<MeshInstance3D>("Cursor");
        VideoStreamPlayer ??= GetNode<VideoStreamPlayer>("Video/VideoViewport/VideoStreamPlayer");
    }

    public override void _Process(double delta)
    {
        // more reliable
        ulong now = Time.GetTicksUsec();
        delta = (now - lastFrame) / 1000000;
        lastFrame = now;

        if (!Playing) return;
        if (firstFrame) { firstFrame = false; return; }

        Attempt.Progress += delta * 1000 * Speed;

        // De-sync corrector

        if (Attempt.Progress > 0 && Attempt.Progress < Attempt.Length && !Attempt.Stopped)
        {
            double audioDelay = Attempt.Progress - settings.LocalOffset - (1000 * (SoundManager.Song.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix()));

            // if de-sync is over 40 milliseconds, then slightly adjust the speed of the song until under 40 milliseconds
            if (Math.Abs(audioDelay / Speed) > Math.Max(40, delta))
            {
                SoundManager.Song.PitchScale = (float)Math.Clamp(Speed + audioDelay / 1000, Math.Max(0.01, Speed - 0.5), Speed + 0.5);
            }
            else if (Math.Abs(SoundManager.Song.PitchScale - Speed) > Mathf.Epsilon)
            {
                SoundManager.Song.PitchScale = (float)Speed;
            }
        }

        // Save replay frame

        // if not paused & record replays on & not a temporary map & time from now and last replay frame was 60 frames apart
        if (!Attempt.Stopped && settings.RecordReplays && !Attempt.Map.Ephemeral && now - Attempt.LastReplayFrame >= 1000000 / 60)
        {
            if (Attempt.ReplayFrames.Count == 0 || (Attempt.ReplayFrames[^1][1..2] != new float[] { Attempt.CursorPosition.X, Attempt.CursorPosition.Y }))
            {
                Attempt.LastReplayFrame = now;
                Attempt.ReplayFrames.Add([
                    (float)Attempt.Progress,
                    Attempt.CursorPosition.X,
                    Attempt.CursorPosition.Y
                ]);
            }
        }

        // Song state check

        if (Attempt.Map.AudioBuffer != null)
        {
            double offsetProgress = Attempt.Progress - settings.LocalOffset;
            double songEnd = SoundManager.Song.Stream.GetLength() * 1000;

            if (!SoundManager.Song.Playing && offsetProgress >= 0 && offsetProgress < songEnd)
            {
                SoundManager.Song.Play((float)offsetProgress / 1000f);
            }
            else if (SoundManager.Song.Playing && offsetProgress >= songEnd)
            {
                SoundManager.Song.Stop();
            }
        }

        // Skip check

        int passedNotes = ObjectIndicesStart[typeof(Note)];
        int nextNoteMillisecond = passedNotes >= Attempt.Map.Notes.Length ? int.MaxValue : Attempt.Map.Notes[passedNotes].Millisecond;

        if (nextNoteMillisecond - Attempt.Progress >= Constants.BREAK_TIME * Speed)
        {
            int lastNoteMillisecond = passedNotes > 0 ? Attempt.Map.Notes[passedNotes - 1].Millisecond : 0;
            int skipWindow = nextNoteMillisecond - Constants.BREAK_TIME - lastNoteMillisecond;

            // only allow skipping if i'm gonna allow it for at least 1 second
            if (skipWindow >= 1000 * Speed)
            {
                if (!Attempt.CanSkip)
                {
                    Attempt.CanSkip = true;
                    EmitSignal(SignalName.SkipAvailable, Attempt);
                }
            }
        }
        else
        {
            Attempt.CanSkip = false;
        }

        // Object processing

        ProcessObjects();
        RenderObjects(delta);

        if (StopQueued || Attempt.Progress >= Attempt.Length && !Attempt.IsReplay)
        {
            StopQueued = false;
            Stop();
        }
    }

    public void ProcessObjects()
    {
        foreach (var entry in Attempt.Objects)
        {
            var type = entry.Key;
            var objects = entry.Value;

            // hopefully no more than 2^31
            int startIndex = ObjectIndicesStart[type];

            ObjectIndicesEnd[type] = objects.Count;

            for (int i = startIndex; i < objects.Count; i++)
            {
                var obj = objects[i];

                if (obj.DoProcess(this))
                {
                    obj.Process(this);
                }
                else
                {
                    // don't waste time iterating over objects past the approach time
                    if (obj.Millisecond > Attempt.Progress + settings.ApproachTime * 1000 * Speed)
                    {
                        ObjectIndicesEnd[type] = Math.Max(obj.Index, ObjectIndicesStart[type]);
                        break;
                    }

                    if (obj.Index == ObjectIndicesStart[type] && obj.Millisecond < Math.Max(Attempt.Progress, Attempt.StartFrom))
                    {
                        ObjectIndicesStart[type]++;
                    }
                }
            }
        }
    }

    public void RenderObjects(double delta)
    {
        foreach (var renderer in Renderers)
        {
            renderer.Process(delta, Attempt);
        }
    }

    public void Play()
    {
        if (Attempt == null) return;

        Map = Attempt.Map;
        Speed = Attempt.Speed;

        ObjectIndicesStart = [];
        ObjectIndicesEnd = [];

        foreach (var entry in Attempt.Objects)
        {
            ObjectIndicesStart[entry.Key] = 0;
            ObjectIndicesEnd[entry.Key] = entry.Value.Count;
        }

        if (!NotesOnly)
        {
            HudManager.Init();
            Attempt.TimeStarted = Time.GetTicksUsec();

            if (!eventsConnected)
            {
                eventsConnected = true;
                HitResultChanged += onHitResultChanged;
            }

            EmitSignal(SignalName.AttemptStatsUpdated, Attempt);
        }

        foreach (var mod in Attempt.Modifiers)
        {
            mod.Active = false;

            if (mod is IMapModifier || mod is IObjectRenderModifier<Note>)
            {
                mod.Activate(Attempt);
                HudManager.DisplayModifier(mod);

                if (mod is IMapModifier mapMod)
                {
                    mapMod.ModifyMap(Attempt.Map, Attempt);
                }
            }
        }

        foreach (var renderer in Renderers)
        {
            renderer.Setup(Attempt.Settings, SkinManager.Instance.Skin);
        }

        settings = Attempt.IsReplay ? Attempt.Replays[0].Settings : SettingsManager.Instance.Settings;
        Camera.Fov = (float)settings.FoV;

        // temp until skinning support
        (Renderers[0] as NoteRenderer).NoteMultiMesh.Multimesh.Mesh = SkinManager.Instance.Skin.NoteMesh;

        SoundManager.BeginGameplayScope(Attempt.Map);
        SoundManager.UpdateVolume();

        if (Attempt.Map.AudioBuffer != null)
        {
            SoundManager.Song.Stream = Util.Audio.LoadStream(Attempt.Map.AudioBuffer);
            SoundManager.Song.PitchScale = (float)Speed;
        }

        if (Attempt.IsReplay)
        {
            for (int i = 0; i < Attempt.Replays.Length; i++)
            {
                Attempt.Replays[i].FrameIndex = 0;
            }
        }

        Playing = true;
        firstFrame = true;

        Logger.Log($"Playing map {Attempt.Map.Name}");
    }

    public void Pause(bool? pause = null)
    {
        Playing = pause ?? !Playing;
        SoundManager.Song.PitchScale = (float)Speed;
        SoundManager.Song.StreamPaused = !Playing;

        if (Playing)
        {
            syncSongPosition();
        }
    }

    public void Skip()
    {
        if (Attempt.CanSkip)
        {
            Attempt.ReplaySkips.Add((float)Attempt.Progress);

            int passedNotes = ObjectIndicesStart[typeof(Note)];

            if (passedNotes >= Attempt.Map.Notes.Length)
            {
                Stop();
            }
            else
            {
                Seek(Attempt.Map.Notes[passedNotes].Millisecond - settings.ApproachTime * 1500 * Speed); // turn AT to ms and multiply by 1.5x)
            }
        }
    }

    public void Seek(double ms)
    {
        Attempt.Progress = ms;

        foreach (var entry in Attempt.Objects)
        {
            ObjectIndicesStart[entry.Key] = 0;
            ObjectIndicesEnd[entry.Key] = entry.Value.Count;
        }

        ProcessObjects();
        RenderObjects(0);

        syncSongPosition();

        // Discord.Client.UpdateEndTime(DateTime.UtcNow.AddSeconds((Time.GetUnixTimeFromSystem() + (Attempt.Map.Length - Attempt.Progress) / 1000 / Speed)));
    }

    public void Fail()
    {
        if (Attempt.Alive)
        {
            SoundManager.FailSound.Play();
        }

        if (!Attempt.IsReplay)
        {
            Attempt.Alive = false;
            Attempt.Qualifies = false;

            if (Attempt.DeathTime == -1)
            {
                Attempt.DeathTime = Math.Max(0, Attempt.Progress);
            }
        }
    }

    public void GiveUp()
    {
        Fail();
        Stop();
    }

    public void QueueStop()
    {
        if (!Playing)
        {
            return;
        }

        Playing = false;
        StopQueued = true;
    }

    public void Stop(bool results = true)
    {
        if (Attempt.Stopped)
        {
            return;
        }

        // give objects a last chance
        ProcessObjects();

        Playing = false;
        StopQueued = false;
        Attempt.Stopped = true;

        if (eventsConnected)
        {
            eventsConnected = false;
            HitResultChanged -= onHitResultChanged;
        }

        // dont want an infinite dependency loop so im just going to do this -fog
        if (!Attempt.IsReplay && Game.Instance.ReplayManager.CurrentMode == ReplayManager.Mode.RECORD)
        {
            Game.Instance.ReplayManager.SaveReplay(Attempt);
        }

        if (!Attempt.IsReplay && !Rhythia.TempMode)
        {
            Stats.Instance.GamePlaytime += (Time.GetTicksUsec() - Attempt.TimeStarted) / 1000000;
            Stats.Instance.TotalDistance += (ulong)Attempt.DistanceMM;

            if (Attempt.StartFrom == 0)
            {
                if (!File.Exists($"{Constants.USER_FOLDER}/pbs/{Attempt.Map.Name}"))
                {
                    List<byte> bytes = [0, 0, 0, 0];
                    bytes.AddRange(SHA256.HashData([0, 0, 0, 0]));
                    File.WriteAllBytes($"{Constants.USER_FOLDER}/pbs/{Attempt.Map.Name}", [.. bytes]);
                }

                Dictionary<string, bool> mods = [];

                foreach (var mod in Attempt.Modifiers)
                {
                    mods[mod.Name] = true;
                }

                Leaderboard leaderboard = new(Attempt.Map.Name, $"{Constants.USER_FOLDER}/pbs/{Attempt.Map.Name}");

                leaderboard.Add(new(Attempt.ID, "You", Attempt.Qualifies, Attempt.Score, Attempt.Accuracy, Time.GetUnixTimeFromSystem(), Attempt.Progress, Attempt.Map.Length, Speed, mods));
                leaderboard.Save();

                if (Attempt.Qualifies)
                {
                    Stats.Instance.Passes++;
                    Stats.Instance.TotalScore += Attempt.Score;

                    if (Attempt.Accuracy == 100)
                    {
                        Stats.Instance.FullCombos++;
                    }

                    if (Attempt.Score > Stats.Instance.HighestScore)
                    {
                        Stats.Instance.HighestScore = Attempt.Score;
                    }

                    Stats.Instance.AverageAccuracy = (Stats.Instance.AverageAccuracy + Attempt.Accuracy) / Stats.Instance.Passes;
                }
            }

            Stats.Instance.ForceUpdate();
        }

        if (results)
        {
            SceneManager.Load("res://scenes/results.tscn");
        }
    }

    private void onHitResultChanged(int noteIndex, HitResult hitResult)
    {
        float lateness = Attempt.IsReplay ? Attempt.HitsInfo[noteIndex] : (float)(((int)Attempt.Progress - Attempt.Map.Notes[noteIndex].Millisecond) / Speed);
        float factor = 1 - Math.Max(0, lateness - 25) / 150f;
        uint hitScore = (uint)(100 * Attempt.ComboMultiplier * Attempt.ModsMultiplier * factor * ((Speed - 1) / 2.5 + 1));

        switch (hitResult)
        {
            case HitResult.Hit:
                Attempt.Hits++;
                Attempt.Sum++;
                Attempt.Accuracy = Math.Floor((float)Attempt.Hits / Attempt.Sum * 10000) / 100;
                Attempt.Combo++;
                Attempt.ComboMultiplierProgress++;
                Attempt.LastHitColour = SkinManager.Instance.Skin.NoteColors[noteIndex % SkinManager.Instance.Skin.NoteColors.Length];
                Attempt.Score += hitScore;

                if (!Attempt.IsReplay)
                {
                    Stats.Instance.NotesHit++;
                    if (Attempt.Combo > Stats.Instance.HighestCombo) Stats.Instance.HighestCombo = Attempt.Combo;

                    Attempt.HitsInfo[noteIndex] = lateness;
                }

                if (Attempt.ComboMultiplierProgress == Attempt.ComboMultiplierIncrement)
                {
                    if (Attempt.ComboMultiplier < 8)
                    {
                        Attempt.ComboMultiplierProgress = Attempt.ComboMultiplier == 7 ? Attempt.ComboMultiplierIncrement : 0;
                        Attempt.ComboMultiplier++;
                    }
                }

                break;
            case HitResult.Miss:
                Attempt.Misses++;
                Attempt.Sum++;
                Attempt.Accuracy = Mathf.Floor((float)Attempt.Hits / Attempt.Sum * 10000) / 100;
                Attempt.Combo = 0;
                Attempt.ComboMultiplierProgress = 0;
                Attempt.ComboMultiplier = Math.Max(1, Attempt.ComboMultiplier - 1);

                if (!Attempt.IsReplay)
                {
                    Stats.Instance.NotesMissed++;
                    Attempt.HitsInfo[noteIndex] = -1;
                }

                break;
            default:
                break;
        }

        if (hitResult != HitResult.None)
        {
            bool hit = hitResult == HitResult.Hit;

            updateHealth(hit);

            if (!Attempt.IsReplay && Attempt.Health <= 0 && Attempt.Alive)
            {
                Fail();
            }

            if (checkFail(hit, Attempt.Health))
            {
                QueueStop();
            }
        }

        EmitSignal(SignalName.AttemptStatsUpdated, Attempt);
    }

    private void updateHealth(bool hit)
    {
        if (Attempt.HasHealthModifier)
        {
            foreach (var mod in Attempt.Modifiers.Where(mod => mod is IHealthModifier))
            {
                Attempt.Health = (mod as IHealthModifier).ApplyHealthResult(hit, Attempt.Health);

                if (!mod.Active)
                {
                    mod.Activate(Attempt);
                    HudManager.DisplayModifier(mod);
                }
            }
        }
        else
        {
            if (hit)
            {
                Attempt.HealthStep = Math.Max(Attempt.HealthStep / 1.45, 15);
                Attempt.Health = Math.Min(100, Attempt.Health + Attempt.HealthStep / 1.75);
            }
            else
            {
                Attempt.Health = Math.Max(0, Attempt.Health - Attempt.HealthStep);
                Attempt.HealthStep = Math.Min(Attempt.HealthStep * 1.2, 100);
            }
        }
    }

    private bool checkFail(bool hit, double health)
    {
        bool defaultFail = health <= 0;
        bool? fail = null;

        foreach (var mod in Attempt.Modifiers)
        {
            if (mod is IFailModifier failMod)
            {
                fail = failMod.CheckFailCondition(hit, health);

                if (fail != defaultFail && !mod.Active)
                {
                    mod.Activate(Attempt);
                    HudManager.DisplayModifier(mod);
                }

                if (fail == true)
                {
                    break;
                }
            }
        }

        return fail ?? defaultFail;
    }

    private void syncSongPosition()
    {
        if (Attempt.Map.AudioBuffer != null)
        {
            if (!SoundManager.Song.Playing && Playing)
            {
                SoundManager.Song.Play();
            }

            SoundManager.Song.Seek((float)(Attempt.Progress - Attempt.Settings.LocalOffset) / 1000);
            VideoStreamPlayer.StreamPosition = (float)Attempt.Progress / 1000;
        }
    }
}
