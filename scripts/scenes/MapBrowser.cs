using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Timer = Godot.Timer;

public partial class MapBrowser : Control
{
    public static MapBrowser Instance;

    public bool Shown;

    private Button hideButton;
    private Panel holder;
    private VBoxContainer topBar;
    private HBoxContainer searchBar;
    private LineEdit search;
    private OptionButton sort;
    private Button direction;
    private Button refresh;
    private MenuButton difficulty;
    private OptionButton status;
    private OptionButton installed;
    private Timer searchTimer;
    private HBoxContainer filters;
    private ScrollContainer results;
    private MapCard mapCardTemplate;

    private CancellationTokenSource source = new();

    public override void _Ready()
    {
        Instance = this;

        hideButton = GetNode<Button>("Hide");
        holder = GetNode<Panel>("Holder");
        topBar = holder.GetNode<VBoxContainer>("Background/Layout/TopBarBackground/Margin/TopBar");
        searchBar = topBar.GetNode<HBoxContainer>("SearchBar");
        search = searchBar.GetNode<LineEdit>("Search");
        sort = searchBar.GetNode<OptionButton>("Sort");
        direction = searchBar.GetNode<Button>("Direction");
        refresh = searchBar.GetNode<Button>("Refresh");
        difficulty = topBar.GetNode<HBoxContainer>("Filters").GetNode<MenuButton>("Difficulty");
        status = topBar.GetNode<HBoxContainer>("Filters").GetNode<OptionButton>("Status");
        installed = topBar.GetNode<HBoxContainer>("Filters").GetNode<OptionButton>("Installed");
        searchTimer = GetNode<Timer>("SearchTimer");
        results = holder.GetNode<ScrollContainer>("Background/Layout/Results");
        mapCardTemplate = results.GetNode<MapCard>("RowsMargin/Rows/MapCardTemplate");

        mapCardTemplate.Visible = false;

        Shown = false;
        Visible = false;

        search.TextChanged += _ => searchTimer.Start();
        searchTimer.Timeout += onSearchTimerTimeout;
        sort.ItemSelected += _ => refreshResults();
        direction.Pressed += toggleDirection;
        refresh.Pressed += refreshResults;
        difficulty.GetPopup().IdPressed += toggleDifficulty;
        status.ItemSelected += _ => refreshResults();
        installed.ItemSelected += _ => refreshResults();

        hideButton.Pressed += HideMenu;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
            return;
        if (!Shown)
            return;
        ShowMenu(false);
        GetViewport().SetInputAsHandled();
    }

    public void ShowMenu(bool show = true)
    {
        Shown = show;
        hideButton.MouseFilter = show ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;

        CallDeferred("move_to_front");

        if (Shown)
        {
            Visible = true;
            holder.OffsetTop = 10;
            holder.OffsetBottom = 10;

            clearResults();
            _ = populate(buildQueryParameters());
        }

        Tween tween = CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out).SetParallel();
        tween.TweenProperty(this, "modulate", Color.Color8(255, 255, 255, (byte)(Shown ? 255 : 0b0)), 0.25);
        tween.TweenProperty(holder, "offset_top", Shown ? 0 : 10, 0.25);
        tween.TweenProperty(holder, "offset_bottom", Shown ? 0 : 10, 0.25);
        tween
            .Chain()
            .TweenCallback(
                Callable.From(() =>
                {
                    Visible = Shown;
                })
            );
    }

    public void HideMenu()
    {
        ShowMenu(false);
    }

    private async Task populate(MapQueryParameters queryParameters)
    {
        source = new CancellationTokenSource();

        JsonElement[] maps;

        try
        {
            maps = await MapBrowserService.Search(queryParameters);
        }
        catch (Exception exception)
        {
            await ToastNotification.Notify("Failed to load maps", 2);
            Logger.Error(exception);
            return;
        }

        foreach (var map in maps.Where(matchesInstalledFilter))
        {
            if (mapCardTemplate.Duplicate() is not MapCard mapCard)
            {
                continue;
            }

            mapCard.Visible = true;
            mapCardTemplate.GetParent().AddChild(mapCard);
            mapCard.Bind(map, source);
        }
    }

    private void onSearchTimerTimeout()
    {
        refreshResults();
    }

    private void toggleDifficulty(long id)
    {
        PopupMenu popup = difficulty.GetPopup();
        int index = popup.GetItemIndex((int)id);
        if (index < 0)
            return;

        popup.SetItemChecked(index, !popup.IsItemChecked(index));
        refreshResults();
    }

    private MapQueryParameters buildQueryParameters()
    {
        int[] selectedDifficulties = Enumerable
            .Range(0, difficulty.GetPopup().ItemCount)
            .Where(index => difficulty.GetPopup().IsItemChecked(index))
            .Select(index => difficulty.GetPopup().GetItemId(index))
            .ToArray();

        string[] statuses = status.Selected switch
        {
            0 => ["UNRANKED"],
            1 => ["RANKED"],
            _ => Array.Empty<string>(),
        };

        return new MapQueryParameters
        {
            Query = search.Text.Trim(),
            SortBy = sort.Selected switch
            {
                1 => "length",
                2 => "difficulty",
                _ => "createdAt",
            },
            SortDirection = direction.ButtonPressed ? "asc" : "desc",
            Difficulties = selectedDifficulties,
            Status = statuses,
        };
    }

    private bool matchesInstalledFilter(JsonElement map)
    {
        int installedFilter = installed.GetSelectedId();
        if (installedFilter == 2)
            return true;

        bool downloaded =
            map.TryGetProperty("noteHash", out var noteHash) && MapManager.Maps.Any(localMap => localMap.ObjectHash == noteHash.GetString());
        return installedFilter == 0 ? downloaded : !downloaded;
    }

    private void toggleDirection()
    {
        direction.ButtonPressed = !direction.ButtonPressed;
        direction.Text = direction.ButtonPressed ? "Ascending" : "Descending";
        refreshResults();
    }

    private void refreshResults()
    {
        clearResults();
        _ = populate(buildQueryParameters());
    }

    private void clearResults()
    {
        source.Cancel();
        source.Dispose();

        var parent = mapCardTemplate.GetParent();
        foreach (Node child in parent.GetChildren())
        {
            if (child != mapCardTemplate)
            {
                child.QueueFree();
            }
        }
    }
}
