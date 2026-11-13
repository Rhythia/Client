using System;
using Godot;
using util;

public partial class QuitButton : Button
{
    private OptionPopup popup;

    public override void _Ready()
    {
        popup = new(I18n.Tr("Quit"), $"[center]{I18n.Tr("Are you sure?")}", true);

        popup.AddOption(I18n.Tr("Quit"), Callable.From(quit));
        popup.AddOption(I18n.Tr("Cancel"), Callable.From(popup.Hide));

        Pressed += () =>
        {
            popup.Show();
        };
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
