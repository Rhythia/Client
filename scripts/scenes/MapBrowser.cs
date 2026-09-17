using System;
using System.Globalization;
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
    private Timer searchTimer;
    private HBoxContainer filters;
    private ScrollContainer results;
    private PanelContainer mapCardTemplate;

    private CancellationTokenSource source = new();

    public override void _Ready()
    {
        Instance = this;

        hideButton = GetNode<Button>("Hide");
        holder = GetNode<Panel>("Holder");
        topBar = holder.GetNode<VBoxContainer>("Background/Layout/TopBarBackground/Margin/TopBar");
        searchBar = topBar.GetNode<HBoxContainer>("SearchBar");
        search = searchBar.GetNode<LineEdit>("Search");
        searchTimer = GetNode<Timer>("SearchTimer");
        results = holder.GetNode<ScrollContainer>("Background/Layout/Results");
        mapCardTemplate = results.GetNode<PanelContainer>("RowsMargin/Rows/MapCardTemplate");

        mapCardTemplate.Visible = false;

        Shown = false;
        Visible = false;

        search.TextChanged += _ => searchTimer.Start();
        searchTimer.Timeout += onSearchTimerTimeout;

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

        foreach (var map in maps)
        {
            if (mapCardTemplate.Duplicate() is not PanelContainer mapCard)
            {
                continue;
            }

            mapCard.Visible = true;
            mapCardTemplate.GetParent().AddChild(mapCard);

            Button downloadButton = mapCard.GetNode<Button>("Download");
            Panel downloadDim = downloadButton.GetNode<Panel>("Dim");
            TextureRect downloadIcon = downloadButton.GetNode<TextureRect>("DownloadIcon");
            Tween downloadTween = null;

            TextureRect coverImage = mapCard.GetNode<TextureRect>("Row/CoverHolder/Cover");
            TextureRect blurCoverImage = mapCard.GetNode<TextureRect>("Row/Map/BlurCoverHolder/BlurCover");

            VBoxContainer info = mapCard.GetNode<VBoxContainer>("Row/Map/Content/Info");
            VBoxContainer topText = info.GetNode<VBoxContainer>("Top/Text");
            HBoxContainer bottomText = info.GetNode<HBoxContainer>("Bottom");

            Label titleLabel = topText.GetNode<Label>("TitleHolder/Title");
            Label mappersLabel = topText.GetNode<Label>("Details/VBoxContainer/Mapper");
            PanelContainer notablePill = bottomText.GetNode<PanelContainer>("NotablePill");
            Label difficultyLabel = topText.GetNode<Label>("Details/VBoxContainer/Difficulty");
            Label noteCountLabel = topText.GetNode<Label>("Details/VBoxContainer/Notes");
            PanelContainer rankingPill = bottomText.GetNode<PanelContainer>("RankingPill");
            Label rankingLabel = rankingPill.GetNode<Label>("Ranking");
            Label durationLabel = bottomText.GetNode<Label>("Duration");

            downloadIcon.PivotOffset = downloadIcon.Size / 2;

            downloadButton.MouseEntered += () =>
            {
                downloadTween?.Kill();
                downloadTween = downloadButton.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut).SetParallel();
                downloadTween.TweenProperty(downloadDim, "modulate", Color.Color8(255, 255, 255, 100), 0.2);
                downloadTween.TweenProperty(downloadIcon, "modulate", Color.Color8(255, 255, 255), 0.2);
            };
            downloadButton.MouseExited += () =>
            {
                downloadTween?.Kill();
                downloadTween = downloadButton.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut).SetParallel();
                downloadTween.TweenProperty(downloadDim, "modulate", Color.Color8(255, 255, 255, 0), 0.2);
                downloadTween.TweenProperty(downloadIcon, "modulate", Color.Color8(255, 255, 255, 0), 0.2);
            };
            downloadButton.Pressed += async () =>
            {
                downloadButton.Disabled = true;
                downloadIcon.Texture = GD.Load<Texture2D>("res://textures/throbber.png");

                Tween throbberTween = downloadIcon.CreateTween().SetLoops();
                throbberTween.TweenProperty(downloadIcon, "rotation", Mathf.Tau, 1).AsRelative();

                await downloadMap(map.GetProperty("fileUrl"), map.GetProperty("legacyId").GetString(), source);

                throbberTween.Kill();
                downloadIcon.Rotation = 0;
                downloadIcon.Texture = GD.Load<Texture2D>("res://user/skins/default/ui/buttons/import.png");
                downloadButton.Disabled = false;
            };

            _ = loadCover(map.GetProperty("covers").GetProperty("128"), coverImage, blurCoverImage, source);

            int difficulty = map.GetProperty("difficulty").GetInt32();
            string difficultyName = map.GetProperty("difficultyName").GetString();
            string difficultyText = string.IsNullOrEmpty(difficultyName) ? Constants.DIFFICULTIES[difficulty] : difficultyName;
            bool isRanked = map.GetProperty("isRanked").GetBoolean();
            var rankingPillStyle = (StyleBoxFlat)rankingPill.GetThemeStylebox("panel").Duplicate();
            var duration = TimeSpan.FromMilliseconds(map.GetProperty("length").GetDouble());

            titleLabel.Text = $"{map.GetProperty("artist").GetString()} - {map.GetProperty("title").GetString()}";
            mappersLabel.Text = $"by {string.Join(", ", map.GetProperty("mappers").EnumerateArray().Select(x => x.GetProperty("name").GetString()))}";
            notablePill.Visible = map.GetProperty("mappers").EnumerateArray().Any(x => x.GetProperty("isNotable").GetBoolean());
            difficultyLabel.Text = difficultyText;
            difficultyLabel.LabelSettings = (LabelSettings)difficultyLabel.LabelSettings.Duplicate();
            difficultyLabel.LabelSettings.FontColor = Constants.DIFFICULTY_COLORS[difficulty];
            noteCountLabel.Text = $"{map.GetProperty("noteCount").GetInt32().ToString(CultureInfo.InvariantCulture)} notes";
            rankingLabel.Text = isRanked ? "RANKED" : "UNRANKED";

            rankingPillStyle.BgColor = isRanked ? Constants.RANKED_COLOR : Constants.UNRANKED_COLOR;
            rankingPill.AddThemeStyleboxOverride("panel", rankingPillStyle);

            durationLabel.Text =
                duration.TotalHours >= 1
                    ? duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                    : duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
        }
    }

    private static async Task loadCover(JsonElement coverUrl, TextureRect coverTexture, TextureRect blurCoverTexture, CancellationTokenSource source)
    {
        var token = source.Token;

        Texture2D cover = null;

        if (!token.IsCancellationRequested && coverUrl.ValueKind != JsonValueKind.Null)
        {
            var coverImage = await MapBrowserService.GetCoverImage(coverUrl.GetString(), token);
            cover = ImageTexture.CreateFromImage(coverImage);
        }

        if (!(IsInstanceValid(coverTexture) && IsInstanceValid(blurCoverTexture)))
            return;

        coverTexture.Texture = cover;
        blurCoverTexture.Texture = cover;
    }

    private static async Task downloadMap(JsonElement fileUrl, string mapId, CancellationTokenSource source)
    {
        var token = source.Token;

        byte[] buffer;

        try
        {
            buffer = await MapBrowserService.GetMapFile(fileUrl.GetString(), token);
        }
        catch (Exception exception)
        {
            await ToastNotification.Notify("Failed to download map", 2);
            Logger.Error(exception);
            return;
        }

        try
        {
            var map = MapParser.PHXM(buffer, mapId);
            MapParser.Encode(map);
            Callable.From(() => MapParser.Instance.EmitSignal(MapParser.SignalName.MapsImportFinished, new[] { map })).CallDeferred();
            _ = ToastNotification.Notify("Map downloaded");
        }
        catch (Exception exception)
        {
            await ToastNotification.Notify("Map is corrupted", 2);
            Logger.Error(exception);
        }
    }

    private void onSearchTimerTimeout()
    {
        clearResults();
        _ = populate(buildQueryParameters());
    }

    private MapQueryParameters buildQueryParameters()
    {
        return new MapQueryParameters { Query = search.Text.Trim() };
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
