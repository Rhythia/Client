using System;
using System.IO;
using Godot;
using SQLite;
using SQLitePCL;

public partial class DatabaseService : Node
{
    public static readonly string DATABASE_PATH = Constants.USER_FOLDER + "/data.db";

    public static SQLiteConnection Connection { get; set; }

    public override void _Ready()
    {
        Batteries.Init();
        Connection = new SQLiteConnection(DATABASE_PATH);
    }

    public override void _ExitTree()
    {
        Connection.Dispose();
    }
}
