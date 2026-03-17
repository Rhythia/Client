using System;
using Godot;

public partial class QuitButton : Button
{
    private OptionPopup popup;

    public override void _Ready()
    {
        popup = new("Quit", "[center]Are you sure?");

        popup.AddOption("Quit", Callable.From(quit));
        popup.AddOption("Cancel", Callable.From(popup.Hide));

        Pressed += () => { popup.Show(); };
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed)
        {
            switch (eventKey.Keycode)
            {
                case Key.Escape:
                    if (SceneManager.Scene is MainMenu mainMenu && mainMenu.CurrentMenu == mainMenu.HomeMenu)
                    {
                        popup.Show();
                    }
                    break;
            }
        }
    }

    private void quit()
    {
        SceneManager.Root.PropagateNotification((int)NotificationWMCloseRequest);
    }
}
