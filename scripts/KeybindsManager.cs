using System;
using Godot;

public partial class KeybindsManager : Node
{
    private string wikiLink = "https://wiki.rhythia.net";
    private OptionPopup wikiPopup;

    public override void _Ready()
    {
        wikiPopup = new("Open Wiki", string.Format(LinkPopupButton.InfoTemplate, wikiLink));

        wikiPopup.AddOption("Open", Callable.From(() => { OS.ShellOpen(wikiLink); }), wikiLink);
        wikiPopup.AddOption("Cancel", Callable.From(wikiPopup.Hide));
    }

    public override void _Input(InputEvent @event)
    {
        var settings = SettingsManager.Instance.Settings;

        if (@event is InputEventKey eventKey && eventKey.Pressed)
        {
            switch (eventKey.Keycode)
            {
                case Key.F1:
                {
                    if (!LegacyRunner.Playing)
                    {
                        wikiPopup.Show(!wikiPopup.Shown);
                    }
                    break;
                }
                default:
                {
                    if (eventKey.Keycode == Key.F11 || (eventKey.AltPressed && (eventKey.Keycode == Key.Enter || eventKey.Keycode == Key.KpEnter)))
                    {
                        bool value = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed;

                        settings.Fullscreen.Value = value;
                    }
                    break;
                }
            }
        }
    }
}
