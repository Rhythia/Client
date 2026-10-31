using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Timer = Godot.Timer;

public partial class MapBrowser : Control
{
    public static MapBrowser Instance;

    public bool Shown;

    public bool IsDragging => dragging;

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
    private CheckBox verified;
    private CheckBox notable;
    private Timer searchTimer;
    private HBoxContainer filters;
    private ScrollContainer results;
    private MapCard mapCardTemplate;
    private OptionButton browseMode;
    private HBoxContainer pagination;
    private Button previous;
    private Button next;
    private Label page;
    private SortDirection sortDirection;
    private int offset;
    private int total;
    private bool stalled;
    private double scrollMomentum;
    private double scrollTarget;
    private double scrollPosition;
    private bool dragCandidate;
    private bool dragging;
    private float dragStartY;
    private float lastDragY;
    private bool isScrolling => browseMode.Selected == 1;

    private enum SortDirection
    {
        Descending,
        Ascending,
    }

    // multiple of 3 because the results grid has 3 columns
    private const int page_size = 18;

    private const int load_more_margin = 240;

    private const float scroll_step = 1500;
    private const float scroll_friction = 20;
    private const float scroll_smoothing = 12;
    private const float drag_threshold = 6;
    private const float drag_scroll_factor = 30;

    private CancellationTokenSource source = new();
    private CancellationTokenSource loadingSource;

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
        verified = topBar.GetNode<HBoxContainer>("Filters").GetNode<CheckBox>("Verified");
        notable = topBar.GetNode<HBoxContainer>("Filters").GetNode<CheckBox>("Notable");
        searchTimer = GetNode<Timer>("SearchTimer");
        results = holder.GetNode<ScrollContainer>("Background/Layout/Results");
        mapCardTemplate = results.GetNode<MapCard>("RowsMargin/Rows/MapCardTemplate");
        browseMode = searchBar.GetNode<OptionButton>("BrowseMode");
        pagination = holder.GetNode<HBoxContainer>("Background/Layout/Pagination");
        previous = pagination.GetNode<Button>("Previous");
        next = pagination.GetNode<Button>("Next");
        page = pagination.GetNode<Label>("Page");

        mapCardTemplate.Visible = false;
        pagination.Visible = !isScrolling;

        Shown = false;
        Visible = false;
        sortDirection = SortDirection.Descending;
        direction.Text = "Descending";

        search.TextChanged += _ => searchTimer.Start();
        searchTimer.Timeout += onSearchTimerTimeout;
        sort.ItemSelected += _ => refreshResults();
        direction.Pressed += toggleDirection;
        refresh.Pressed += onRefresh;
        browseMode.ItemSelected += _ => onBrowseModeChanged();
        results.GetVScrollBar().ValueChanged += _ => loadMoreIfNeeded();
        results.GetVScrollBar().Changed += loadMoreIfNeeded;
        results.GuiInput += onResultsInput;
        difficulty.GetPopup().IdPressed += toggleDifficulty;
        status.ItemSelected += _ => refreshResults();
        verified.Toggled += _ => refreshResults();
        notable.Toggled += _ => refreshResults();
        previous.Pressed += () => changePage(-page_size);
        next.Pressed += () => changePage(page_size);

        hideButton.Pressed += HideMenu;
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;

        var scrollBar = results.GetVScrollBar();

        if (Math.Abs(results.ScrollVertical - scrollPosition) > 1)
        {
            scrollPosition = results.ScrollVertical;
            scrollTarget = scrollPosition;
            scrollMomentum = 0;
            return;
        }

        if (dragCandidate)
        {
            if (!Input.IsMouseButtonPressed(MouseButton.Left))
            {
                endDrag();
            }
            else
            {
                float mouseY = GetViewport().GetMousePosition().Y;

                if (!dragging && Math.Abs(mouseY - dragStartY) > drag_threshold)
                {
                    dragging = true;
                    updateCardHover();
                }

                if (dragging)
                    scrollMomentum += (lastDragY - mouseY) * drag_scroll_factor;

                lastDragY = mouseY;
            }
        }

        scrollMomentum = Mathf.Lerp(scrollMomentum, 0, Math.Min(1, scroll_friction * delta));
        scrollTarget = Math.Clamp(scrollTarget + scrollMomentum * delta, 0, Math.Max(0, scrollBar.MaxValue - scrollBar.Page));
        scrollPosition = Mathf.Lerp(scrollPosition, scrollTarget, Math.Min(1, scroll_smoothing * delta));
        results.ScrollVertical = (int)scrollPosition;
    }

    private void onResultsInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } mouseButton)
            return;

        switch (mouseButton.ButtonIndex)
        {
            case MouseButton.WheelDown:
                scrollMomentum += scroll_step;
                break;
            case MouseButton.WheelUp:
                scrollMomentum -= scroll_step;
                break;
            default:
                return;
        }

        GetViewport().SetInputAsHandled();
    }

    private void startDrag(Vector2 position)
    {
        if (!Shown || !results.GetGlobalRect().HasPoint(position) || results.GetVScrollBar().GetGlobalRect().HasPoint(position))
            return;

        dragCandidate = true;
        dragStartY = position.Y;
        lastDragY = position.Y;
    }

    private void endDrag()
    {
        bool wasDragging = dragging;

        dragCandidate = false;
        dragging = false;

        if (wasDragging)
            updateCardHover();
    }

    private void updateCardHover()
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();

        foreach (Node child in mapCardTemplate.GetParent().GetChildren())
        {
            if (child is MapCard card && card != mapCardTemplate)
                card.SetHovered(!dragging && card.GetGlobalRect().HasPoint(mousePosition));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } click)
        {
            if (click.Pressed)
                startDrag(click.GlobalPosition);
            else
                endDrag();

            return;
        }

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

            if (isScrolling)
                offset = 0;

            stalled = false;
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

    private async Task populate(MapQueryParameters queryParameters, bool append = false)
    {
        CancellationTokenSource populationSource = append ? source : new();
        source = populationSource;
        loadingSource = populationSource;

        try
        {
            await loadResults(queryParameters, populationSource);
        }
        finally
        {
            if (loadingSource == populationSource)
                loadingSource = null;
        }
    }

    private async Task loadResults(MapQueryParameters queryParameters, CancellationTokenSource populationSource)
    {
        MapSearchResult searchResult;

        try
        {
            searchResult = await MapBrowserService.Search(queryParameters);
        }
        catch (OperationCanceledException) when (populationSource.IsCancellationRequested)
        {
            return;
        }
        catch (RateLimitedException exception)
        {
            stalled = true;
            updatePaginationButtons();

            string wait = exception.RetryAfter is { } retryAfter ? $"in {(int)Math.Ceiling(retryAfter.TotalSeconds)}s" : "later";
            _ = ToastNotification.Notify($"Rate limited, try again {wait}", 2);
            return;
        }
        catch (Exception exception)
        {
            stalled = true;
            updatePaginationButtons();
            _ = ToastNotification.Notify("Failed to load maps", 2);
            Logger.Error(exception);
            return;
        }

        if (populationSource.IsCancellationRequested)
            return;

        total = searchResult.Total;

        if (isScrolling)
            offset += searchResult.Maps.Length;
        else
            updatePagination(searchResult.Maps.Length);

        foreach (var map in searchResult.Maps)
        {
            if (mapCardTemplate.Duplicate() is not MapCard mapCard)
            {
                continue;
            }

            mapCard.Visible = true;
            mapCardTemplate.GetParent().AddChild(mapCard);

            try
            {
                mapCard.Bind(map, populationSource);
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                mapCard.QueueFree();
            }
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
            Limit = page_size,
            Notable = notable.ButtonPressed,
            Verified = verified.ButtonPressed,
            Offset = offset,
            Query = search.Text.Trim(),
            SortBy = sort.Selected switch
            {
                1 => "length",
                2 => "difficulty",
                _ => "createdAt",
            },
            SortDirection = sortDirection switch
            {
                SortDirection.Ascending => "asc",
                _ => "desc",
            },
            Difficulties = selectedDifficulties,
            Status = statuses,
        };
    }

    private void toggleDirection()
    {
        sortDirection = sortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
        direction.Text = sortDirection.ToString();
        refreshResults();
    }

    private void refreshResults()
    {
        offset = 0;
        loadPage();
    }

    private void changePage(int delta)
    {
        offset = Math.Max(offset + delta, 0);
        loadPage();
    }

    private void onRefresh()
    {
        if (isScrolling)
            refreshResults();
        else
            loadPage();
    }

    private void onBrowseModeChanged()
    {
        pagination.Visible = !isScrolling;
        refreshResults();
    }

    private void loadMoreIfNeeded()
    {
        if (!isScrolling || stalled || loadingSource != null || offset >= total)
            return;

        var scrollBar = results.GetVScrollBar();
        if (scrollBar.Value + scrollBar.Page < scrollBar.MaxValue - load_more_margin)
            return;

        _ = populate(buildQueryParameters(), true);
    }

    private void loadPage()
    {
        stalled = false;
        previous.Disabled = true;
        next.Disabled = true;
        results.ScrollVertical = 0;

        clearResults();
        _ = populate(buildQueryParameters());
    }

    private void updatePaginationButtons()
    {
        previous.Disabled = offset == 0;
        next.Disabled = offset + page_size >= total;
    }

    private void updatePagination(int shown)
    {
        updatePaginationButtons();
        page.Text = shown == 0 ? "0-0 of 0" : $"{offset + 1}-{offset + shown} of {total}";
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
