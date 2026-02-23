using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public partial class MapList : Panel, ISkinnable
{
    public static MapList Instance;

    public enum ListLayout { List, Grid }

    [ExportGroup("Layout")]

    [Export]
    public ListLayout Layout = ListLayout.List;

    [Export]
    public float Spacing = 10;

    [ExportGroup("Button Sizing")]

    [Export]
    public float WideButtonMinimumSize = 90;

    [Export]
    public float WideButtonHoveredSize = 10;

    [Export]
    public float WideButtonSelectedSize = 20;

    [Export]
    public float SquareButtonMinimumSize = 140;

    [Export]
    public float SquareButtonHoveredSize = 5;

    [Export]
    public float SquareButtonSelectedSize = 10;

    [ExportGroup("Scrolling")]

    [Export]
    public float ScrollStep = 1500;

    [Export]
    public float ScrollFriction = 20;

    [Export]
    public float ScrollElasticity = 0.02f;

    public double ScrollLength = 0;
    public double ScrollMomentum = 0;
    public double TargetScroll = 0;
    public double Scroll = 0;
    public bool DragScroll = false;
    public bool MouseScroll = false;
    public bool DisplaySelectionCursor = false;
    public string SearchQuery = "";
    public string AuthorQuery = "";

    /// <summary>
    /// Queried and ordered maps to display in the list
    /// </summary>
    public List<Map> Maps = [];

    private TextureRect mask;
    private TextureRect selectionCursor;
    private Panel scrollBar;
    private Panel scrollBarMain;
    private TextureRect scrollBarMainTop;
    private TextureRect scrollBarMainMiddle;
    private TextureRect scrollBarMainBottom;
    private Panel scrollBarBackground;
    private TextureRect scrollBarBackgroundTop;
    private TextureRect scrollBarBackgroundMiddle;
    private TextureRect scrollBarBackgroundBottom;

    private readonly PackedScene mapButtonContainerTemplate = ResourceLoader.Load<PackedScene>("res://prefabs/map_button_container.tscn");
    private readonly PackedScene mapButtonWideTemplate = ResourceLoader.Load<PackedScene>("res://prefabs/map_button_wide.tscn");
    private readonly PackedScene mapButtonSquareTemplate = ResourceLoader.Load<PackedScene>("res://prefabs/map_button_square.tscn");

    private Dictionary<int, HBoxContainer> containers = [];
    private Stack<HBoxContainer> containerCache = [];
    private Dictionary<string, MapButton> mapButtons = [];
    private Stack<MapButton> mapButtonCache = [];
    private MapButton hoveredButton;
    private string selectedMapID;
    private float buttonMinSize = 0;
    private float buttonHoverSize = 0;
    private float buttonSelectSize = 0;
    private int buttonsPerContainer = 1;
    private Vector2 lastMousePos = Vector2.Zero;
    private float dragDistance = 0;

    public override void _Ready()
    {
        Instance = this;

        mask = GetNode<TextureRect>("Mask");
        selectionCursor = GetNode<TextureRect>("SelectionCursor");
        scrollBar = GetNode<Panel>("ScrollBar");
        scrollBarMain = scrollBar.GetNode<Panel>("Main");
        scrollBarMainTop = scrollBarMain.GetNode<TextureRect>("Top");
        scrollBarMainMiddle = scrollBarMain.GetNode<TextureRect>("Middle");
        scrollBarMainBottom = scrollBarMain.GetNode<TextureRect>("Bottom");
        scrollBarBackground = scrollBar.GetNode<Panel>("Background");
        scrollBarBackgroundTop = scrollBarBackground.GetNode<TextureRect>("Top");
        scrollBarBackgroundMiddle = scrollBarBackground.GetNode<TextureRect>("Middle");
        scrollBarBackgroundBottom = scrollBarBackground.GetNode<TextureRect>("Bottom");

        MouseExited += () => { toggleSelectionCursor(false); };
        Resized += clear;
        SkinManager.Instance.Loaded += UpdateSkin;
        MapParser.Instance.MapsImportFinished += maps => {
            MapCache.Load(false);
            UpdateMaps();
            Select(maps[0]);
        };
        MapManager.MapsInitialized += _ => UpdateMaps();
        MapManager.MapUpdated += map => {
            UpdateMaps();
        };

        Task.Run(() => UpdateMaps());

        UpdateLayout(Layout);
        UpdateSkin();
    }

    public override void _Process(double delta)
    {
        buttonsPerContainer = Layout == ListLayout.List ? 1 : (int)(Size.X / (buttonMinSize + Spacing));

        float scrollElasticOffset = 0;

        if ((TargetScroll <= 0 && ScrollMomentum < 0) || (TargetScroll >= ScrollLength && ScrollMomentum > 0))
        {
            scrollElasticOffset = (float)(ScrollMomentum * ScrollElasticity);
        }

        ScrollLength = Math.Max(0, Maps.Count / buttonsPerContainer * (buttonMinSize + Spacing) - Spacing - Size.Y) + buttonHoverSize + buttonSelectSize;
        ScrollMomentum = Mathf.Lerp(ScrollMomentum, 0.0, Math.Min(1, ScrollFriction * delta));

        if (Layout == ListLayout.Grid)
        {
            ScrollLength += buttonMinSize + buttonHoverSize + buttonSelectSize;
        }

        Vector2 mousePos = DisplayServer.MouseGetPosition();

        if (DragScroll && IsVisibleInTree())
        {
            float dragDelta = (lastMousePos.Y - mousePos.Y) * 30;

            ScrollMomentum += dragDelta;
            dragDistance += Math.Abs(dragDelta);
        }

        lastMousePos = mousePos;

        if (MouseScroll)
        {
            float t = Mathf.InverseLerp(Position.Y + scrollBarMain.Size.Y / 2, Position.Y + Size.Y - scrollBarMain.Size.Y / 2, GetViewport().GetMousePosition().Y);
            TargetScroll = Mathf.Lerp(TargetScroll, ScrollLength * Math.Clamp(t, 0, 1), Math.Min(1, 24 * delta));
        }
        else
        {
            TargetScroll = Math.Clamp(TargetScroll + ScrollMomentum * delta, 0, ScrollLength) + scrollElasticOffset;
        }

        Scroll = Mathf.Lerp(Scroll, TargetScroll, Math.Min(1, 12 * delta));

        scrollBarMain.AnchorTop = (float)Math.Max(0, (TargetScroll - scrollElasticOffset) / (ScrollLength + Size.Y));
        scrollBarMain.AnchorBottom = (float)Math.Min(1, scrollBarMain.AnchorTop + Size.Y / (ScrollLength + Size.Y));

        HBoxContainer lastContainer = null;
        List<HBoxContainer> drawnContainers = [];
        List<float> containerSizeOffsets = [];
        int firstIndex = -1;
        float upOffset = 0;
        float downOffset = 0;

        for (int i = 0; i < Maps.Count; i++)
        {
            int containerIndex = i / buttonsPerContainer;
            Map map = Maps[i];
            float top = containerIndex * (buttonMinSize + Spacing) - (float)Scroll;
            float bottom = top + buttonMinSize;
            bool display = top < Size.Y && bottom > 0;
            MapButton button = mapButtons.TryGetValue(map.Name, out MapButton value) ? value : null;

            // Cache/ignore if outside map list

            if (!display)
            {
                if (button != null)
                {
                    var parentContainer = button.Container;

                    foreach (MapButton buttonSibling in parentContainer.GetChildren())
                    {
                        buttonSibling.Container = null;

                        if (buttonSibling == hoveredButton)
                        {
                            hoveredButton = null;
                        }

                        mapButtons.Remove(buttonSibling.Map.Name);
                        mapButtonCache.Push(buttonSibling);
                        parentContainer.RemoveChild(buttonSibling);
                        buttonSibling.Deselect();
                        buttonSibling.UpdateOutline(0f, 0f);
                    }

                    if (parentContainer.GetParent() == mask)
                    {
                        mask.RemoveChild(parentContainer);
                    }

                    containers.Remove(containerIndex);
                    containerCache.Push(parentContainer);
                }

                continue;
            }

            // Everything must be rendered from here

            if (!containers.TryGetValue(containerIndex, out HBoxContainer container))
            {
                container = containerCache.Count > 0 ? containerCache.Pop() : mapButtonContainerTemplate.Instantiate<HBoxContainer>();

                container.AddThemeConstantOverride("separation", (int)Spacing);
                containers[containerIndex] = container;
                mask.AddChild(container);
            }

            if (button == null)
            {
                button = mapButtonCache.Count > 0 ? mapButtonCache.Pop() : setupButton(Layout == ListLayout.List ? mapButtonWideTemplate.Instantiate<MapButtonWide>() : mapButtonSquareTemplate.Instantiate<MapButtonSquare>());

                button.ListIndex = i;
                button.Container = container;
                mapButtons[map.Name] = button;
                container.AddChild(button);
                button.UpdateInfo(map, map.Name == selectedMapID);
            }

            if (container != lastContainer)
            {
                drawnContainers.Add(container);

                if (firstIndex == -1)
                {
                    firstIndex = containerIndex;
                }

                float sizeOffset = container.Size.Y > 0 ? container.Size.Y - buttonMinSize : 0;

                downOffset += sizeOffset;
                containerSizeOffsets.Add(sizeOffset);

                lastContainer = container;
            }
        }

        Vector2 selectionCursorPos = new(-80, selectionCursor.Position.Y);

        if (IsInstanceValid(hoveredButton) && hoveredButton != null && hoveredButton.IsInsideTree())
        {
            selectionCursorPos = new(
                DisplaySelectionCursor ? hoveredButton.Holder.Position.X - 60 : -80,
                Math.Clamp(hoveredButton.Container.Position.Y + hoveredButton.Size.Y / 2 - selectionCursor.Size.Y / 2, 0, Size.Y)
            );
        }

        selectionCursor.Position = selectionCursor.Position.Lerp(selectionCursorPos, (float)Math.Min(1, 8 * delta));
        selectionCursor.Rotation = -selectionCursor.Position.Y / 60;

        for (int i = 0; i < drawnContainers.Count; i++)
        {
            int index = i + firstIndex;
            var container = drawnContainers[i];

            float sizeOffset = containerSizeOffsets[i];
            float indexOffset = index * (buttonMinSize + Spacing);
            float top = indexOffset - (float)Scroll + (buttonMinSize + buttonHoverSize + buttonSelectSize) / 2;

            downOffset -= sizeOffset;
            top += (upOffset - downOffset) / 2;
            upOffset += sizeOffset;

            container.ZIndex = index == 0 || index == Math.Ceiling(Maps.Count / (float)buttonsPerContainer) - 1 ? 1 : 0;
            container.Position = new(container.Position.X, top);
            container.OffsetBottom = container.OffsetTop;
            container.OffsetLeft = 0;
            container.OffsetRight = 0;

            foreach (MapButton button in container.GetChildren())
            {
                button.LightPosition = DisplaySelectionCursor ? selectionCursor.GlobalPosition : new(-10000, Size.Y / 2);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey && eventKey.Pressed)
        {
            switch (eventKey.Keycode)
            {
                case Key.F2:
                    shuffle();
                    break;
                case Key.Space:
                    Control focused = GetViewport().GuiGetFocusOwner();

					if (Lobby.Map != null && IsVisibleInTree() && focused is not LineEdit)
					{
                        LegacyRunner.Play(Lobby.Map, Lobby.Speed, Lobby.StartFrom, Lobby.Modifiers);
                    }
                    break;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && !mouseButton.CtrlPressed && !mouseButton.AltPressed)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Left: DragScroll = mouseButton.Pressed; if (DragScroll) { dragDistance = 0; } break;
                case MouseButton.Right: MouseScroll = mouseButton.Pressed; if (MouseScroll) { dragDistance = 0; } break;
                case MouseButton.WheelDown: ScrollMomentum += ScrollStep; break;
                case MouseButton.WheelUp: ScrollMomentum -= ScrollStep; break;
            }
        }
    }

    public void Select(Map map, bool playIfPreSelected = true)
    {
        if (selectedMapID != null && selectedMapID != map.Name && mapButtons.TryGetValue(selectedMapID, out MapButton value))
        {
            value.Deselect();
            value.UpdateOutline(0f);
        }

        MapManager.Select(map);

        if (selectedMapID == map.Name && playIfPreSelected)
        {
            LegacyRunner.Play(Lobby.Map, Lobby.Speed, Lobby.StartFrom, Lobby.Modifiers);
        }

        selectedMapID = map.Name;

        Focus(map);

        SceneManager.Space.UpdateMap(map);
    }

    public void Focus(Map map)
    {
        TargetScroll = Maps.FindIndex(otherMap => otherMap.Name == map.Name) / buttonsPerContainer * (buttonMinSize + Spacing) + buttonMinSize / 2 - Size.Y / 2;

        if (SceneManager.Scene is MainMenu mainMenu)
        {
            mainMenu.Transition(mainMenu.PlayMenu);
        }
    }

    public void Search(string query = "", string author = "")
    {
        SearchQuery = query ?? SearchQuery;
        AuthorQuery = author ?? AuthorQuery;

        UpdateMaps();
    }

    public void UpdateMaps()
    {
        Maps.Clear();

        List<Map> queried = [.. MapManager.Maps.Where(x => x.PrettyTitle.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase) && x.PrettyMappers.Contains(AuthorQuery, StringComparison.CurrentCultureIgnoreCase))];
        List<Map> unfavorited = [];

        foreach (Map map in queried)
        {
            (map.Favorite ? Maps : unfavorited).Add(map);
        }

        foreach (Map map in unfavorited)
        {
            Maps.Add(map);
        }

        clear();
    }

    public void UpdateLayout(ListLayout layout)
    {
        if (Layout != layout)
        {
            Layout = layout;
            Scroll = 0;
            TargetScroll = 0;
        }

        buttonMinSize = layout == ListLayout.List ? WideButtonMinimumSize : SquareButtonMinimumSize;
        buttonHoverSize = layout == ListLayout.List ? WideButtonHoveredSize : SquareButtonHoveredSize;
        buttonSelectSize = layout == ListLayout.List ? WideButtonSelectedSize : SquareButtonSelectedSize;

        clear();
    }

    public void UpdateSkin(SkinProfile skin = null)
    {
        skin ??= SkinManager.Instance.Skin;

        mask.Texture = skin.MapListMaskImage;
        selectionCursor.Texture = skin.MapListSelectionCursorImage;
        scrollBarMainTop.Texture = skin.MapListScrollBarTopImage;
        scrollBarMainMiddle.Texture = skin.MapListScrollBarMiddleImage;
        scrollBarMainBottom.Texture = skin.MapListScrollBarBottomImage;
        scrollBarBackgroundTop.Texture = skin.MapListScrollBarBackgroundTopImage;
        scrollBarBackgroundMiddle.Texture = skin.MapListScrollBarBackgroundMiddleImage;
        scrollBarBackgroundBottom.Texture = skin.MapListScrollBarBackgroundBottomImage;
    }

    private MapButton setupButton(MapButton button)
    {
        button.MinimumSize = buttonMinSize;
        button.HoveredSizeOffset = buttonHoverSize;
        button.SelectedSizeOffset = buttonSelectSize;

        button.MouseHovered += (hovered) => {
            if (hovered)
            {
                hoveredButton = button;
                if (Layout == ListLayout.List) { toggleSelectionCursor(true); }
            }

            if (button.Map.Name != selectedMapID)
            {
                button.UpdateOutline(hovered ? 0.5f : 0);
            }
        };
        button.Pressed += () => {
            if (dragDistance < 500)
            {
                Select(button.Map);

                button.Select();
                button.UpdateOutline(1.0f);
            }
        };

        return button;
    }

    private void toggleSelectionCursor(bool display)
    {
        if (DisplaySelectionCursor == display) { return; }

        DisplaySelectionCursor = display;

        if (display && hoveredButton != null)
        {
            selectionCursor.Position = new(selectionCursor.Position.X, hoveredButton.Position.Y);
        }

        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetParallel();
        tween.TweenProperty(selectionCursor, "modulate", Color.Color8(255, 255, 255, (byte)(display ? 255 : 0)), 0.1);
    }

    private void clear()
    {
        hoveredButton = null;

        foreach (KeyValuePair<string, MapButton> entry in mapButtons)
        {
            entry.Value.QueueFree();
        }
        foreach (MapButton button in mapButtonCache)
        {
            button.QueueFree();
        }
        foreach (KeyValuePair<int, HBoxContainer> entry in containers)
        {
            entry.Value.QueueFree();
        }
        foreach (HBoxContainer container in containerCache)
        {
            container.QueueFree();
        }

        mapButtons.Clear();
        mapButtonCache.Clear();
        containers.Clear();
        containerCache.Clear();
    }

    private void shuffle()
    {
        Maps = [.. Maps.Shuffle()];

        clear();
    }
}
