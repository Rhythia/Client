using System;
using Godot;

public abstract class CameraMode
{
    public abstract string Name { get; }

    public abstract bool Rankable { get; }

    public abstract void Process(
        Attempt attempt,
        ReplayManager replayManager,
        Camera3D camera,
        MeshInstance3D cursor,
        Vector2 mouseDelta,
        float sensitivity
    );
}
