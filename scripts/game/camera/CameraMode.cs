using System;
using Godot;

public abstract class CameraMode
{
    public abstract string Name { get; }

    public abstract bool Rankable { get; }

    public abstract void Process(Attempt attempt, Camera3D camera, Vector2 mouseDelta);
}
