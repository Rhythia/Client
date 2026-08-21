using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;

public partial class ReplayManager : Node
{
    public enum Mode
    {
        NONE,
        RECORD,
        PLAYBACK
    }

    [Export] public Runner Runner { get; set; }
    [Export] public Mode CurrentMode { get; set; }
    [Export] public Panel ReplayViewer { get; set; }
    [Export] public CursorManager CursorManager { get; private set; }

    public bool ViewerVisible;

    // only public variable because of Game
    public static TextureButton SeekerPause;
    private static Label seekerTime;
    private static HSlider seekerTimeline;
    private static bool seekerHovered;
    public float ReplayLength;
    public string ReplayPath;
    public Vector2 CursorPosition { get; private set; }

    private Godot.FileAccess file;
    private ulong statusOffset, frameCountOffset;

    public void NewReplay(Attempt attempt)
    {
        var settings = attempt.Settings;

        if (!settings.RecordReplays || Rhythia.TempMode) return;

        ReplayPath = $"{Constants.USER_FOLDER}/replays/{attempt.ID}.phxr";

        file = Godot.FileAccess.Open(ReplayPath, Godot.FileAccess.ModeFlags.Write);

        file.StoreString("phxr");  // sig
        file.Store8(1);    // replay file version

        file.StoreDouble(attempt.Speed);
        file.StoreDouble(attempt.StartFrom);
        file.StoreDouble(settings.ApproachRate);
        file.StoreDouble(settings.ApproachDistance);
        file.StoreDouble(settings.FadeIn);
        // file.Store8((byte)(settings.FadeOut ? 1 : 0));
        file.Store8((byte)(settings.FadeOut > 0 ? 1 : 0));
        file.Store8((byte)(settings.Pushback ? 1 : 0));
        file.StoreDouble(settings.CameraParallax);
        file.StoreDouble(settings.FoV.Value);
        file.StoreDouble(settings.NoteSize);
        file.StoreDouble(settings.Sensitivity);

        statusOffset = (uint)file.GetPosition();
        file.Store8(0);

        var mods = Runner.Attempt.Modifiers.Select(mod => mod.Name);

        // temp until replays are redone
        if (Runner.Attempt.CameraMode.Name == "Spin")
        {
            mods = mods.Append("Spin");
        }

        string serializedMods = string.Join("_", mods);
        string mapName = Path.GetFileName(attempt.Map.FolderPath);
        string player = "You";

        void storeSizedString(string data)
        {
            file.Store32((uint)data.Length);
            file.StoreString(data);
        }

        storeSizedString(serializedMods);
        storeSizedString(mapName);
        file.Store64((ulong)attempt.Map.Notes.Length);
        storeSizedString(player);

        frameCountOffset = (uint)file.GetPosition();
        file.Store64(0);   // reserve frame count
    }

    public void SaveReplay(Attempt attempt)
    {
        if (file == null || !file.IsOpen()) { return; }

        file.Seek(statusOffset);
        file.Store8((byte)(attempt.Alive ? (attempt.Qualifies ? 0 : 1) : 2));
        file.Seek(frameCountOffset);
        file.Store64((ulong)attempt.ReplayFrames.Count);

        foreach (float[] frame in attempt.ReplayFrames)
        {
            file.StoreFloat(frame[0]);
            file.StoreFloat(frame[1]);
            file.StoreFloat(frame[2]);
        }

        file.Seek(file.GetLength());
        file.Store64(attempt.FirstNote);
        file.Store64(attempt.Sum);

        int low = Math.Max(0, (int)attempt.FirstNote);
        int high = Math.Clamp((int)(attempt.FirstNote + attempt.Sum), low, int.MaxValue);
        float[] hitsInfo = attempt.HitsInfo[low..high];

        for (int i = 0; i < hitsInfo.Length; i++)
        {
            file.Store8((byte)(hitsInfo[i] == -1 ? 255 : Math.Min(254, hitsInfo[i] * (254 / 55))));
        }

        file.Store64((ulong)attempt.ReplaySkips.Count);

        foreach (float skip in attempt.ReplaySkips)
        {
            file.StoreFloat(skip);
        }

        file.Close();

        // open replay to store hash
        file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/replays/{attempt.ID}.phxr", Godot.FileAccess.ModeFlags.ReadWrite);
        ulong length = file.GetLength();
        byte[] hash = SHA256.HashData(file.GetBuffer((long)length));
        file.StoreBuffer(hash);

        file.Close();

        attempt.ReplayPath = ReplayPath;
    }

    public void InitReplayLength()
    {
        if (Runner?.Attempt == null || !Runner.Attempt.IsReplay) return;
        ReplayLength = Runner.Attempt.MaxReplayLength;
    }

    public override void _Ready()
    {
        base._Ready();

        // this entire code lowkey sucks, so i am just copy and pasting it because i am lazy -fog
        SeekerPause = ReplayViewer.GetNode<TextureButton>("Pause");
        seekerTime = ReplayViewer.GetNode<Label>("Time");
        seekerTimeline = ReplayViewer.GetNode<HSlider>("Seek");
        CursorManager ??= GetNode<CursorManager>("CursorManager");

        SeekerPause.Pressed += TogglePause;

        seekerTimeline.ValueChanged += value =>
        {
            string current = $"{Util.String.FormatTime(value * ReplayLength / 1000)}";
            string end = $"{Util.String.FormatTime(ReplayLength / 1000)}";
            seekerTime.Text = $"{current} / {end}";
        };

        seekerTimeline.DragEnded += _ =>
        {
            seek((float)seekerTimeline.Value);
            seekerTimeline.ReleaseFocus();
        };

        seekerTimeline.FocusEntered += () =>
        {
            seekerHovered = true;
        };
        seekerTimeline.FocusExited += () =>
        {
            seekerHovered = false;
        };
    }

    public override void _Process(double delta)
    {
        if (!Runner.Attempt.IsReplay || !Runner.Playing) return;

        if (!seekerHovered)
        {
            seekerTimeline.Value = Runner.Attempt.Progress / Runner.Attempt.MaxReplayLength;

            if (Runner.Attempt.Progress > ReplayLength && Runner.Playing)
            {
                TogglePause();
            }
        }

        updateCursors();
    }

    public void ShowReplayViewer(Attempt attempt, bool? show = null)
    {
        ViewerVisible = show ?? !ViewerVisible;
        bool visible = ViewerVisible && attempt.IsReplay;

        ReplayViewer.Visible = visible;

        if (attempt.IsReplay)
        {
            Input.MouseMode = visible
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Hidden;
        }
    }

    public void TogglePause()
    {
        Runner.Pause();

        string texturePath = Runner.Playing
            ? "res://textures/ui/pause.png"
            : "res://textures/ui/play.png";

        SeekerPause.TextureNormal = GD.Load<Texture2D>(texturePath);
    }

    private void seek(float seekedTime)
    {
        seekedTime *= ReplayLength;

        var att = Runner.Attempt;

        att.Hits = 0;
        att.Misses = 0;
        att.Sum = 0;
        att.Accuracy = 100;
        att.Score = 0;
        att.Combo = 0;
        att.ComboMultiplier = 1;
        att.ComboMultiplierProgress = 0;
        att.Health = 100;
        att.HealthStep = 15;
        att.Alive = true;
        att.Qualifies = true;

        for (int i = 0; i < att.Map.Notes.Length; i++)
        {
            var note = att.Map.Notes[i];

            note.Hittable = false;
            note.LastResult = HitResult.None;

            if (note.Millisecond > Math.Max(att.Progress, seekedTime))
            {
                break;
            }
            else if (note.Millisecond < seekedTime && note.Index >= (int)att.FirstNote)
            {
                bool missed = att.Replays[0].Notes[i] == -1;

                if (missed)
                {
                    note.Miss(Runner, false);
                }
                else
                {
                    note.Hit(Runner, false);
                }
            }
        }

        if (att.Progress > ReplayLength && seekedTime <= ReplayLength && !Runner.Playing)
        {
            TogglePause();
        }

        Runner.Seek(seekedTime);
        Runner.EmitSignal(Runner.SignalName.AttemptStatsUpdated, att);

        for (int i = 0; i < att.Replays[0].Frames.Length; i++)
        {
            if (att.Progress < att.Replays[0].Frames[i].Progress)
            {
                att.Replays[0].FrameIndex = Math.Max(0, i - 1);
                break;
            }
        }

        updateCursors();
    }

    private void updateCursors()
    {
        for (int i = 0; i < Runner.Attempt.Replays.Length; i++)
        {
            var replay = Runner.Attempt.Replays[i];

            if (replay.FrameIndex == replay.Frames.Length - 1) continue;

            // advance frame forward deterministically making sure frames only advance when allowed
            while (replay.FrameIndex < replay.Frames.Length - 1 &&
                   Runner.Attempt.Progress >= replay.Frames[replay.FrameIndex + 1].Progress)
            {
                replay.FrameIndex++;

                if (replay.FrameIndex == replay.Frames.Length - 1)
                {
                    CursorManager.HideCursor(i, false);
                    continue;
                }
            }

            int next = Math.Min(replay.FrameIndex + 1, replay.Frames.Length - 1);

            var currentFrame = replay.Frames[replay.FrameIndex];
            var nextFrame = replay.Frames[next];

            double inverse = Mathf.InverseLerp(
                currentFrame.Progress,
                nextFrame.Progress,
                Runner.Attempt.Progress
            );

            Vector2 cursorPos = currentFrame.CursorPosition.Lerp(
                nextFrame.CursorPosition,
                (float)Math.Clamp(inverse, 0, 1)
            );

            CursorPosition = cursorPos;

            CursorManager.UpdateCursor(CursorPosition, i);
            CursorManager.ShowCursor(i);
        }
    }
}
