using System;
using Godot;

public partial class MapBrowserButton : Button
{
    public override void _Pressed()
    {
        MapBrowser.Instance.ShowMenu(true);
    }
}
