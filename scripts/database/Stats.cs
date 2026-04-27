using System;
using System.IO;
using System.Security.Cryptography;
using Godot;
using Godot.Collections;
using SQLite;

#nullable enable

public class Stats
{
    private const string id = "_STATS";

    public static Stats Instance { get; private set; } = new();

    public static void Initialize()
    {
        DatabaseService.Connection.CreateTable<Stats>();
        SQLiteConnection connection = DatabaseService.Connection;

        Stats? result = connection.Find<Stats>(id);

        if (result == null)
        {
            result = new();
            connection.Insert(result);
        }

        Instance = result;
    }

    [PrimaryKey]
    public string Id { get; set; } = id;

    public ulong GamePlaytime { get; set; }
    public ulong TotalPlaytime { get; set; }
    public ulong GamesOpened { get; set; }
    public ulong TotalDistance { get; set; }
    public ulong NotesHit { get; set; }
    public ulong NotesMissed { get; set; }
    public ulong HighestCombo { get; set; }
    public ulong Attempts { get; set; }
    public ulong Passes { get; set; }
    public ulong FullCombos { get; set; }
    public ulong HighestScore { get; set; }
    public ulong TotalScore { get; set; }
    public ulong RageQuits { get; set; }
    public double AverageAccuracy { get; set; }

    public event Action<Stats>? StatsUpdated;


    /// <summary>
    /// Forces a sync for stats
    /// </summary>
    public void ForceUpdate() => StatsUpdated?.Invoke(this);

    public void Save()
    {
        SQLiteConnection connection = DatabaseService.Connection;
        connection.InsertOrReplace(Instance);
        Logger.Log("Saved stats");
    }
}
