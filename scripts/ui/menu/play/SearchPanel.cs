using Godot;
using System;

public partial class SearchPanel : Panel
{
    [Export]
    public bool SearchAuthor = false;

    private LineEdit lineEdit;
    private TextureRect searchIcon;

    public override void _Ready()
    {
        lineEdit = GetNode<LineEdit>("LineEdit");
        searchIcon = GetNode<TextureRect>("TextureRect");

        lineEdit.TextChanged += (text) => {
            searchIcon.SelfModulate = new Color(1, 1, 1, text == "" ? 0.5f : 1);

            MapList.Instance.Search(SearchAuthor ? null : text, SearchAuthor ? text : null);
        };
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.CtrlPressed && !eventKey.AltPressed)
        {
            if (GetViewport().GuiGetFocusOwner() == null && eventKey.Keycode != Key.Space && eventKey.Keycode != Key.Escape)
            {
                lineEdit.GrabFocus();
            }
        }
        else if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
        {
            lineEdit.ReleaseFocus();
        }
    }
}
