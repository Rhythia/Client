using System;
using Godot;

public partial class LayoutButton : Button
{
    public override void _Pressed()
    {
        var layout = MapList.Instance.Layout == MapList.ListLayout.List ? MapList.ListLayout.Grid : MapList.ListLayout.List;

        Icon = layout == MapList.ListLayout.List ? SkinManager.Instance.Skin.LayoutListButtonImage : SkinManager.Instance.Skin.LayoutGridButtonImage;

        MapList.Instance.UpdateLayout(layout);
    }
}
