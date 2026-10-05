using System;
using System.Collections.Generic;
using System.IO;
using Godot;

public partial class ContextMenu : Panel
{
    public static ContextMenu Instance;

    public static bool Shown { get; private set; }

    public static Map Map { get; private set; }

    private static Dictionary<Button, Action> subscriptions = [];

    private static bool dialogHasAction = false;

    private Button favoriteButton;
    
    private Button deleteButton;

    private Button videoButton;

    [Export]
    private VBoxContainer container;

    [Export]
    private Button templateButton;

    [Export]
    private FileDialog videoDialog;

    public override void _Ready()
    {
        Instance = this;

        container ??= GetNode<VBoxContainer>("Container");
        templateButton ??= GetNode<Button>("Container/TemplateButton");
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (Shown && !GetRect().HasPoint(mouseButton.GlobalPosition))
            {
                Hide();
                GetViewport().SetInputAsHandled();
            }
        }

        if (@event is InputEventKey eventKey && eventKey.Pressed && Shown)
        {
            switch (eventKey.Keycode)
            {
                case Key.Escape:
                    GD.Print("escape handled by context menu");
                    Hide();
                    GetViewport().SetInputAsHandled();
                    break;
            }
        }
    }

    public void SetupButtons()
    {
        favoriteButton?.QueueFree();
        deleteButton?.QueueFree();
        videoButton?.QueueFree();

        favoriteButton = Map.Favorite ? createButton("Unfavorite", Color.Color8(255, 255, 128), Color.Color8(255, 255, 196), unfavoriteMap, templateButton) : createButton("Favorite", Color.Color8(255, 255, 128), Color.Color8(255, 255, 196), favoriteMap, templateButton);

        deleteButton = createButton("Delete Map", Color.Color8(255, 128, 128), Color.Color8(255, 196, 196), deleteMap, templateButton);

        bool hasVideo = File.Exists(Path.Combine(MapUtil.MapsFolder, Map.Name, "video.mp4"));

        videoButton = hasVideo ? createButton("Remove Video", Color.Color8(128, 128, 255), Color.Color8(196, 196, 255), removeVideo, templateButton) : createButton("Add Video", Color.Color8(128, 128, 255), Color.Color8(196, 196, 255), addVideo, templateButton);

        if (!dialogHasAction)
        {
            videoDialog.FileSelected += insertVideo;
            dialogHasAction = true;
        }

        container.AddChild(favoriteButton);
        container.AddChild(deleteButton);
        container.AddChild(videoButton);
    }

    public static void Show(Vector2 pos, Map map, bool show = true)
    {
        Shown = show;
        Map = map;

        // Instance.MoveToFront();
        Instance.ZIndex = 100;

        if (show)
        {
            Instance.SetupButtons();
            Instance.Position = pos;

            Instance.Visible = true;
        }
    }

    public new static void Hide()
    {
        Shown = false;
        Instance.Visible = false;
    }

    private static Button createButton(string text, Color fontColor, Color fontHoverColor, Action clickAction, Button template = null, bool makeVisible = true)
    {
        Button button = template != null ? (Button)template.Duplicate() : new Button();

        button.Text = text;
        button.AddThemeColorOverride("font_color", fontColor);
        button.AddThemeColorOverride("font_hover_color", fontHoverColor);

        addPressedAction(button, clickAction);

        if (makeVisible) { button.Visible = true; }

        return button;
    }

    private static void addPressedAction(Button button, Action action)
    {
        if (subscriptions.ContainsKey(button)) { clearPressedAction(button); }

        button.Pressed += action;
        subscriptions.Add(button, action);
    }

    private static void clearPressedAction(Button button)
    {
        if (subscriptions.Remove(button, out Action subscriber))
        {
            button.Pressed -= subscriber;
        }
        else
        {
            Logger.Log($"No {button} found in ContextMenu subscriptions dict.");
        }
    }

    private void favoriteMap()
    {
        Map.Favorite = true;
        MapManager.Update(Map);
        Hide();
    }

    private void unfavoriteMap()
    {
        Map.Favorite = false;
        MapManager.Update(Map);
        Hide();
    }

    private void addVideo()
    {
        videoDialog.Popup();
        Hide();
    }

    private void removeVideo()
    {
        MapManager.RemoveVideo(Map);
        Hide();
    }

    private void deleteMap()
    {
        MapManager.Delete(Map);
        Hide();
    }

    private void insertVideo(string file)
    {
        MapManager.InsertVideo(Map, file);
    }
}