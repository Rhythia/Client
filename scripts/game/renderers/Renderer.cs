using System;
using System.Collections.Generic;
using Godot;

// The generic is kinda useless right now but keeping it for future use
public abstract partial class Renderer : Node3D
{
    internal SettingsProfile Settings { get; private set; }

    public void ApplySettings(SettingsProfile settings)
    {
        Settings = settings;
    }

    public abstract void Process(double delta, Attempt attempt);
}
