using System;
using Godot;

public partial class SettingsMenuButton : Button
{
    public override void _Pressed()
    {
        SettingsManager.ShowMenu(true);
    }
}
