using System.Globalization;
using Godot;
using util;

public partial class LinkPopupButton : Button
{
    public static string InfoTemplate =
        $"[center][color=dddddd]{I18n.Tr("This will open the following link:{0}")}\n\n[color=dddddd]{I18n.Tr("Are you sure?")}";

    [Export]
    public string Link;

    private OptionPopup popup;

    public override void _EnterTree()
    {
        base._EnterTree();

        popup = new(I18n.Tr("Open Link"), "", true);

        UpdateLink(Link);

        popup.AddOption(
            I18n.Tr("Open"),
            Callable.From(() =>
            {
                OS.ShellOpen(Link);
            }),
            Link
        );
        popup.AddOption(I18n.Tr("Cancel"), Callable.From(popup.Hide));
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        popup?.QueueFree();
    }

    public override void _Ready()
    {
        base._Ready();

        Pressed += Press;
    }

    public void Press()
    {
        popup?.Show();
    }

    public void UpdateLink(string link)
    {
        Link = link;
        string formattedLink = $"\n[color=aaaaff][i][u]{Link}[/u][/i]";

        if (popup != null)
        {
            if (popup.Options.TryGetValue(I18n.Tr("Open"), out Button button))
            {
                button.TooltipText = link;
            }

            popup.UpdateInfo(string.Format(CultureInfo.InvariantCulture, InfoTemplate, formattedLink));
        }
    }
}
