using System;
using Godot;

public partial class RandomPickButton : Button
{
    private Random random = new();

    public override void _Pressed() { Pick(); }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.CtrlPressed)
        {
            switch (key.Keycode)
            {
                case Key.F4:
                    Pick();
                    break;
            }
        }
    }

    public void Pick()
    {
        var mapList = MapList.Instance;
        int index = random.Next(mapList.Maps.Count);

        mapList.Select(mapList.Maps[index], false);
    }
}
