using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Security.Cryptography;
using Godot;

public struct Leaderboard
{
    public string MapID;
    public bool Valid = false;
    public string Path;
    public FileParser FileBuffer;
    public uint ScoreCount;
    public List<Score> Scores;

    public Leaderboard(string mapID, byte[] buffer)
    {
        MapID = mapID;

        Path ??= $"{Constants.USER_FOLDER}/pbs/{MapID}";

        byte[] bytes = [];

        try
        {
            FileBuffer = new(buffer);

            bytes = FileBuffer.Get((int)FileBuffer.Length - 32);
            FileBuffer.Seek(0);
            uint scoreCount = ScoreCount = FileBuffer.GetUInt32();

            Scores = [];

            for (int i = 0; i < scoreCount; i++)
            {
                Add(new(FileBuffer.Get((int)FileBuffer.GetUInt32())));
            }

            Valid = true;
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            Reset();
        }

        if (FileBuffer.Get(32).Stringify() != SHA256.HashData(bytes).Stringify())
        {
            Logger.Log("Leaderboard file corrupted; invalid leaderboard hash");
            Reset();
        }
    }

    public Leaderboard(string mapID, string path)
    {
        if (!File.Exists(path))
        {
            throw Logger.Error($"No leaderboard file found at path {path}");
        }

        Path = path;

        this = new(mapID, File.ReadAllBytes(path));
    }

    public void Add(Score score)
    {
        Scores.Add(score);
        Scores.Sort(new ScoreComparer());

        if (Scores.Count > 8)
        {
            Scores.RemoveRange(8, Scores.Count - 8);
        }

        ScoreCount = (uint)Scores.Count;
    }

    public void Save()
    {
        Godot.FileAccess file = Godot.FileAccess.Open(Path, Godot.FileAccess.ModeFlags.Write);

        file.Store32(ScoreCount);

        foreach (Score score in Scores)
        {
            ulong offset = file.GetPosition();

            file.Store32(0);    // reserved for length
            file.Store32((uint)score.AttemptID.Length);
            file.StoreString(score.AttemptID);
            file.Store32((uint)score.Player.Length);
            file.StoreString(score.Player);
            file.Store8((byte)(score.Qualifies ? 1 : 0));
            file.Store64(score.Value);
            file.StoreDouble(score.Accuracy);
            file.StoreDouble(score.Time);
            file.StoreDouble(score.Progress);
            file.StoreDouble(score.MapLength);
            file.StoreDouble(score.Speed);

            Godot.Collections.Dictionary<string, bool> modifiers = [];

            foreach (KeyValuePair<string, bool> entry in score.Modifiers)
            {
                modifiers[entry.Key] = entry.Value;
            }

            string json = Json.Stringify(modifiers);

            file.Store32((uint)json.Length);
            file.StoreString(json);

            ulong end = file.GetPosition();

            file.Seek(offset);
            file.Store32((uint)(end - offset - 4));
            file.Seek(end);
        }

        file.Close();

        List<byte> hashed = [.. File.ReadAllBytes(Path)];
        hashed.AddRange(SHA256.HashData([.. hashed]));
        File.WriteAllBytes(Path, [.. hashed]);
    }

    public void Reset()
    {
        Valid = false;
        ScoreCount = 0;
        Scores = [];
        Save();
    }

    public struct Score
    {
        public string AttemptID;
        public string Player;
        public bool Qualifies;
        public ulong Value;
        public double Accuracy;
        public double Time;
        public double Progress;
        public double MapLength;
        public double Speed;
        public Dictionary<string, bool> Modifiers;

        public Score(byte[] buffer)
        {
            FileParser FileBuffer = new(buffer);

            AttemptID = FileBuffer.GetString((int)FileBuffer.GetUInt32());
            Player = FileBuffer.GetString((int)FileBuffer.GetUInt32());
            Qualifies = FileBuffer.GetBool();
            Value = FileBuffer.GetUInt64();
            Accuracy = FileBuffer.GetDouble();
            Time = FileBuffer.GetDouble();
            Progress = FileBuffer.GetDouble();
            MapLength = FileBuffer.GetDouble();
            Speed = FileBuffer.GetDouble();
            Modifiers = [];

            foreach (KeyValuePair<string, bool> entry in (Godot.Collections.Dictionary<string, bool>)Json.ParseString(FileBuffer.GetString((int)FileBuffer.GetUInt32())))
            {
                Modifiers[entry.Key] = entry.Value;
            }
        }

        public Score(string id, string player, bool qualifies, ulong value, double accuracy, double time, double progress, double mapLength, double speed, Dictionary<string, bool> modifiers)
        {
            AttemptID = id;
            Player = player;
            Qualifies = qualifies;
            Value = value;
            Accuracy = accuracy;
            Time = time;
            Progress = progress;
            MapLength = mapLength;
            Speed = speed;
            Modifiers = modifiers;
        }

        public override readonly string ToString() => $"{Accuracy}, {Value} by {Player}";
    }

    public struct ScoreComparer : IComparer<Score>
    {
        public int Compare(Score a, Score b)
        {
            return b.Value.CompareTo(a.Value);
        }
    }
}
