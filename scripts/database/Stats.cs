using System;
using System.IO;
using System.Security.Cryptography;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Stats : Node
{
    public static event Action OnSaved;

    public static ulong GamePlaytime = 0;
    public static ulong TotalPlaytime = 0;
    public static ulong GamesOpened = 0;
    public static ulong TotalDistance = 0;
    public static ulong NotesHit = 0;
    public static ulong NotesMissed = 0;
    public static ulong HighestCombo = 0;
    public static ulong Attempts = 0;
    public static ulong Passes = 0;
    public static ulong FullCombos = 0;
    public static ulong HighestScore = 0;
    public static ulong TotalScore = 0;
    public static ulong RageQuits = 0;
    public static Array<double> PassAccuracies = [];
    public static Godot.Collections.Dictionary<string, ulong> FavoriteMaps = [];

    public static void Save()
    {
        File.SetAttributes($"{Constants.USER_FOLDER}/stats", FileAttributes.None);
        Godot.FileAccess file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/stats", Godot.FileAccess.ModeFlags.Write);
        string accuraciesJson = Json.Stringify(PassAccuracies);
        string mapsJson = Json.Stringify(FavoriteMaps);

        file.Store8(1);
        file.Store64(GamePlaytime);
        file.Store64(TotalPlaytime);
        file.Store64(GamesOpened);
        file.Store64(TotalDistance);
        file.Store64(NotesHit);
        file.Store64(NotesMissed);
        file.Store64(HighestCombo);
        file.Store64(Attempts);
        file.Store64(Passes);
        file.Store64(FullCombos);
        file.Store64(HighestScore);
        file.Store64(TotalScore);
        file.Store64(RageQuits);
        file.Store32((uint)accuraciesJson.Length);
        file.StoreString(accuraciesJson);
        file.Store32((uint)mapsJson.Length);
        file.StoreString(mapsJson);
        file.Close();

        byte[] bytes = File.ReadAllBytes($"{Constants.USER_FOLDER}/stats");
        byte[] hash = new byte[32];

        SHA256.HashData(bytes, hash);

        file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/stats", Godot.FileAccess.ModeFlags.Write);
        file.StoreBuffer(bytes);
        file.StoreBuffer(hash);
        file.Close();

        File.SetAttributes($"{Constants.USER_FOLDER}/stats", FileAttributes.Hidden);

        OnSaved?.Invoke();
        Logger.Log("Saved stats");
    }

    public static void Load()
    {
        try
        {
            FileParser file = new($"{Constants.USER_FOLDER}/stats");

            byte[] bytes = file.Get((int)file.Length - 32);

            file.Seek(0);

            byte version = file.Get(1)[0];

            switch (version)
            {
                case 1:
                {
                    GamePlaytime = file.GetUInt64();
                    TotalPlaytime = file.GetUInt64();
                    GamesOpened = file.GetUInt64();
                    TotalDistance = file.GetUInt64();
                    NotesHit = file.GetUInt64();
                    NotesMissed = file.GetUInt64();
                    HighestCombo = file.GetUInt64();
                    Attempts = file.GetUInt64();
                    Passes = file.GetUInt64();
                    FullCombos = file.GetUInt64();
                    HighestScore = file.GetUInt64();
                    TotalScore = file.GetUInt64();
                    RageQuits = file.GetUInt64();
                    PassAccuracies = (Array<double>)Json.ParseString(file.GetString((int)file.GetUInt32()));
                    FavoriteMaps = (Godot.Collections.Dictionary<string, ulong>)Json.ParseString(file.GetString((int)file.GetUInt32()));

                    byte[] hash = file.Get(32);
                    byte[] newHash = new byte[32];

                    SHA256.HashData(bytes, newHash);

                    for (int i = 0; i < 32; i++)
                    {
                        if (hash[i] != newHash[i])
                        {
                            throw new("Wrong hash lol");
                        }
                    }

                    break;
                }
            }
        }
        catch (Exception exception)
        {
            ToastNotification.Notify("Stats file corrupt or modified", 2);
            Logger.Error(exception);
        }

        Logger.Log("Loaded stats");
    }
}
