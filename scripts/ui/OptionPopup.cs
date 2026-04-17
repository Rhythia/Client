using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

public partial class OptionPopup : Control
{
    [Export]
    public string Header = "Header";

    [Export]
    public string Info = "Information goes here";

    public bool Shown = false;
    public Dictionary<string, Button> Options = [];

    [Signal]
    public delegate void CanceledEventHandler();

    private readonly PackedScene template = GD.Load<PackedScene>("res://prefabs/option_popup.tscn");

    [Export]
    private Label headerLabel;
    [Export]
    private RichTextLabel infoLabel;
    [Export]
    private HBoxContainer buttonContainer;
    [Export]
    private Button buttonTemplate;

    public OptionPopup() { }

    public OptionPopup(string header, string info)
    {
        Util.Misc.CopyReference(this, template.Instantiate<OptionPopup>());

        Header = header;
        Info = info;
        Name = $"OptionPopup{new Regex("[^a-zA-Z0-9_-]").Replace(Header, "")}";

        SceneManager.Root.CallDeferred("add_child", this);
    }

    public override void _Ready()
    {
        Node container = GetNode("Holder").GetNode("VBoxContainer");

        headerLabel.Text = Header;
        infoLabel.Text = Info;

        GetNode<Button>("Hide").Pressed += Hide;

        Visible = false;
        Modulate = Color.Color8(255, 255, 255, 0);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed && Shown)
        {
            switch (eventKey.Keycode)
            {
                case Key.Escape:
                    Hide();
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }
    }

    public void AddOption(string text, Callable callback, string tooltip = null)
    {
        Button button = buttonTemplate.Duplicate() as Button;

        button.Text = text;
        button.TooltipText = tooltip;
        button.Visible = true;
        button.Pressed += () =>
        {
            Hide();
            callback.Call();
        };

        buttonContainer.AddChild(button);
        Options[text] = button;
    }

    public void Show(bool show = true)
    {
        Shown = show;

        MoveToFront();

        if (show) { Visible = true; }
        else { EmitSignal(SignalName.Canceled); }

        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad);
        tween.TweenProperty(this, "modulate", Color.Color8(255, 255, 255, (byte)(show ? 255 : 0)), 0.1);
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = Shown;
        }));
    }

    public void Hide()
    {
        Show(false);
    }

    public void UpdateHeader(string header)
    {
        Header = header;
        headerLabel.Text = header;
    }

    public void UpdateInfo(string info)
    {
        Info = info;
        infoLabel.Text = info;
    }


}
