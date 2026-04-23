using System;
using System.Collections.Generic;
using Godot;

public partial class MapInfo : AspectRatioContainer
{
    public static MapInfo Instance;

    public Map Map;
    public MapInfoContainer InfoContainer;

    private MapList mapList;
    private Panel holder;

    private Map pendingSelection;

    private readonly PackedScene infoContainerTemplate = ResourceLoader.Load<PackedScene>("res://prefabs/map_info_container.tscn");
    private Stack<MapInfoContainer> infoContainerCache = [];

    public override void _Ready()
    {
        Instance = this;

        mapList = GetParent().GetNode<MapList>("MapList");
        holder = GetNode<Panel>("Holder");

        MapManager.Selected.ValueChanged += (_, _) => Select(MapManager.Selected.Value);
        MapManager.MapDeleted += map =>
        {
            Callable.From(() =>
            {
                if (Map == null || Map.Name == map.Name)
                {
                    Map = null;
                    InfoContainer?.Transition(false);
                }
            }).CallDeferred();
        };
    }

    public override void _Draw()
    {
        float height = (AnchorBottom - AnchorTop) * GetParent<Control>().Size.Y - OffsetTop + OffsetBottom;

        holder.CustomMinimumSize = Vector2.One * Math.Min(850, height);
    }

    /// <summary>
    /// Called when entering the tree to apply any pending selection that occurred while off-tree.
    /// </summary>
    public void ApplyPendingSelection()
    {
        if (pendingSelection != null)
        {
            var map = pendingSelection;
            pendingSelection = null;
            Map = null; // force re-select
            Select(map);
        }
    }

    public void Select(Map map)
    {
        if (map == null) return;

        // Defer selection if not in the scene tree (e.g. importing from another scene)
        if (!IsInsideTree())
        {
            pendingSelection = map;
            return;
        }

        if (Map != null && map.Name == Map.Name) { return; }

        Map = map;
        pendingSelection = null;

        var oldContainer = InfoContainer;

        InfoContainer?.Transition(false).TweenCallback(Callable.From(() =>
        {
            holder.RemoveChild(oldContainer);
            infoContainerCache.Push(oldContainer);
        }));

        InfoContainer = infoContainerCache.Count > 0 ? infoContainerCache.Pop() : infoContainerTemplate.Instantiate<MapInfoContainer>();

        holder.AddChild(InfoContainer);
        InfoContainer.Setup(map);
        InfoContainer.Transition(true);

        QueueRedraw();
    }
}
