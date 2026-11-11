using System;
using Godot;

public abstract partial class MeshUIComponent3D : MeshInstance3D, IUIComponent
{
    public Runner Runner { get; set; }

    public virtual void Init() { }

    public virtual void OnExitTree() { }

    public override void _ExitTree()
    {
        OnExitTree();
        // QueueFree();
    }

    public virtual void Process(double delta, Attempt attempt) { }

    public virtual void ApplySettings(SettingsProfile settings) { }
}
