using System;
using Godot;

public interface IUIComponent
{
    Runner Runner {get; set;}
    void Init() {}
    void OnExitTree() {}
    void Process(double delta, Attempt attempt) {}
    void ApplySettings(SettingsProfile settings) {}
}

public abstract partial class UIComponent : Node3D, IUIComponent
{
    public Runner Runner {get; set;}

    public virtual void Init() {}

    public virtual void OnExitTree() {}

    public override void _ExitTree()
    {
        OnExitTree();
        QueueFree();
    }

    public virtual void Process(double delta, Attempt attempt) {}

    public virtual void ApplySettings(SettingsProfile settings) {}
}
