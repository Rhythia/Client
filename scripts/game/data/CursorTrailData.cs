using Godot;

public readonly record struct CursorTrailData
{
    public readonly ulong Time;
    public readonly float Rotation;
    public readonly Vector2 Position;

    public CursorTrailData(ulong time, Vector2 position, float rotation)
    {
        Time = time;
        Position = position;
        Rotation = rotation;
    }
}
