using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Godot;

public struct Replay
{
    public string ID;
    public string MapID;
    public ulong MapNoteCount;
    public string MapFilePath;
    public ulong ReplayNoteCount;
    public string Player;

    public FileParser FileBuffer;
    public SettingsProfile Settings;
    public bool Valid;
    public float Length;
    public string Status;

    public double Speed;
    public double StartFrom;
    public ulong FirstNote;
    public CameraMode CameraMode;
    public List<Modifier> Modifiers;
    public double ApproachRate;
    public double ApproachDistance;
    public double ApproachTime;
    public double FadeIn;
    public bool FadeOut;
    public bool Pushback;
    public double Parallax;
    public double FoV;
    public double NoteSize;
    public double Sensitivity;

    public bool Complete;
    public ulong LastNote;
    public ulong LastFrame;
    public Vector2 CurrentPosition;
    public Frame[] Frames;
    public int FrameIndex;
    public float[] Notes;
    public float[] Skips;
    public int SkipIndex;

    public struct Frame(float progress, float x, float y)
    {
        public float Progress = progress;
        public Vector2 CursorPosition = new(x, y);

        public override string ToString() => $"({CursorPosition}) @{Progress}ms";
    }

    public Replay(string path)
    {
        FileBuffer = new(path);
        Valid = true;
        Complete = false;
        LastNote = 0;
        LastFrame = 0;

        // Get the replay's hash
        byte[] bytes = FileBuffer.Get((int)FileBuffer.Length - 32);
        string ReplaySHA256 = FileBuffer.Get(32).Stringify();

        // crc32 is faster, will change later -fog
        if (SHA256.HashData(bytes).Stringify() != ReplaySHA256)
        {
            Valid = false;
            ToastNotification.Notify("Replay file corrupted", 2);
            Logger.Error($"Replay file corrupted; invalid SHA256 hash");
            return;
        }

        FileBuffer.Seek(0);

        string sig = FileBuffer.GetString(4);

        if (sig != "phxr")
        {
            Valid = false;
            ToastNotification.Notify("Replay file corrupted", 2);
            Logger.Error($"Replay file corrupted; invalid file sig {sig}");
            return;
        }

        if (FileBuffer.GetUInt8() == 1)
        {
            ID = path.GetFile().GetBaseName();
            Speed = FileBuffer.GetDouble();
            StartFrom = FileBuffer.GetDouble();
            ApproachRate = FileBuffer.GetDouble();
            ApproachDistance = FileBuffer.GetDouble();
            ApproachTime = ApproachDistance / ApproachRate;
            FadeIn = FileBuffer.GetDouble();
            FadeOut = FileBuffer.GetBool();
            Pushback = FileBuffer.GetBool();
            Parallax = FileBuffer.GetDouble();
            FoV = FileBuffer.GetDouble();
            NoteSize = FileBuffer.GetDouble();
            Sensitivity = FileBuffer.GetDouble();

            Settings = new();
            Settings.ApproachRate.Value = ApproachRate;
            Settings.ApproachDistance.Value = ApproachDistance;
            Settings.ApproachTime.Value = ApproachTime;
            Settings.FadeIn.Value = FadeIn;
            Settings.FadeOut.Value = FadeOut ? 100 : 0;
            Settings.Pushback.Value = Pushback;
            Settings.FoV.Value = FoV;
            Settings.NoteSize.Value = NoteSize;
            Settings.Sensitivity.Value = Sensitivity;

            ushort status = FileBuffer.GetUInt8();
            Status = status == 0 ? "PASSED" : status == 1 ? "DISQUALIFIED" : "FAILED";

            List<string> rawMods = [.. FileBuffer.GetString((int)FileBuffer.GetUInt32()).Split("_")];

            Modifiers = [];
            CameraMode = new CameraLock();

            foreach (string modName in rawMods)
            {
                // i hate this
                switch (modName)
                {
                    case "NoFail":
                        Modifiers.Add(new NoFailModifier());
                        break;
                    case "Ghost":
                        Modifiers.Add(new GhostModifier());
                        break;
                    case "Chaos":
                        Modifiers.Add(new ChaosModifier());
                        break;
                    case "Earthquake":
                        Modifiers.Add(new EarthquakeModifier());
                        break;
                    case "Vortex":
                        Modifiers.Add(new VortexModifier());
                        break;
                    case "VFlip":
                        Modifiers.Add(new VerticalFlipModifier());
                        break;
                    case "HFlip":
                        Modifiers.Add(new HorizontalFlipModifier());
                        break;
                    case "Spin":
                        CameraMode = new CameraSpin();
                        break;
                    default:
                        break;
                }
            }

            // Modifiers = new()
            // {
            //     ["NoFail"] = rawMods.Contains("NoFail"),
            //     ["Ghost"] = rawMods.Contains("Ghost"),
            //     ["Spin"] = rawMods.Contains("Spin"),
            //     ["Flashlight"] = rawMods.Contains("Flashlight"),
            //     ["Chaos"] = rawMods.Contains("Chaos"),
            //     ["HardRock"] = rawMods.Contains("HardRock")
            // };

            MapID = FileBuffer.GetString((int)FileBuffer.GetUInt32());

            MapNoteCount = FileBuffer.GetUInt64();
            MapFilePath = $"{Constants.USER_FOLDER}/maps/{MapID}";
            if (Path.GetExtension(MapFilePath).Equals(".phxm", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Legacy Replay detected: {MapFilePath}");
                MapFilePath = Path.ChangeExtension(MapFilePath, null); // just in case it tries to read a .phxm file

            }

            if (!Directory.Exists(MapFilePath))
            {
                Valid = false;
                ToastNotification.Notify("Replay map not found", 2);
                Logger.Log($"Replay map not found, path: {MapFilePath}");
                return;
            }

            Player = FileBuffer.GetString((int)FileBuffer.GetUInt32());
            Frames = new Frame[FileBuffer.GetUInt64()];
            CurrentPosition = new();
            FrameIndex = 0;
            SkipIndex = 0;

            if (Frames.Length <= 1)
            {
                Valid = false;
                return;
            }

            for (int i = 0; i < Frames.Length; i++)
            {
                Frames[i] = new(FileBuffer.GetFloat(), FileBuffer.GetFloat(), FileBuffer.GetFloat());
            }

            Length = Frames.Length > 0 ? Frames[^1].Progress : 0;
            Notes = new float[MapNoteCount];
            FirstNote = FileBuffer.GetUInt64();
            ReplayNoteCount = FileBuffer.GetUInt64();

            for (ulong i = 0; i < FirstNote; i++)
            {
                Notes[i] = -1;
            }

            for (ulong i = FirstNote; i < FirstNote + ReplayNoteCount; i++)
            {
                ushort note = FileBuffer.GetUInt8();
                Notes[i] = note == 255 ? -1 : note / (254 / 55);
            }

            for (ulong i = FirstNote + ReplayNoteCount; i < MapNoteCount; i++)
            {
                Notes[i] = -1;
            }

            Skips = new float[FileBuffer.GetUInt64()];

            for (int i = 0; i < Skips.Length; i++)
            {
                Skips[i] = FileBuffer.GetFloat();
            }
        }
    }

    public override readonly string ToString() => ID;

    public override readonly bool Equals(object obj)
    {
        return GetHashCode() == obj.GetHashCode();
    }

    public override readonly int GetHashCode()
    {
        int hashCode = 0;
        byte[] bytes = $"{MapID}{Valid}{Speed}{StartFrom}{Modifiers}".ToUtf16Buffer();
        byte[] hash = SHA256.HashData(bytes);

        for (int i = 0; i < hash.Length; i += 4)
        {
            hashCode += BitConverter.ToInt32(hash, i);  // this is so ass
        }

        return hashCode;
    }

    public static bool operator ==(Replay left, Replay right) => left.Equals(right);

    public static bool operator !=(Replay left, Replay right) => !(left == right);
}
