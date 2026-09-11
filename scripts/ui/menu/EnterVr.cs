using System;
using Godot;

public partial class EnterVr : Button
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (VRNode.IsVrEnabled)
        {
            ((MarginContainer)GetParent()).Visible = false;
        }

    }

    public override void _Pressed()
    {
        // reopen app with --xr-mode on
        OS.CreateInstance(new string[] { "--xr-mode", "on" });
        SceneManager.Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}
