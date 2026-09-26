using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public partial class MapCard : PanelContainer
{
    private Control mapHolder;
    private Control rightPanelContainer;
    private Control rightPanelCorners;

    private MarginContainer contentContainer;

    private VBoxContainer info;
    private VBoxContainer topText;
    private HBoxContainer bottomText;

    private PanelContainer coverHolder;
    private PanelContainer blurCoverHolder;
    private PanelContainer rankingPill;
    private PanelContainer notablePill;

    private Panel coverDim;
    private Panel blurCoverDim;
    private Panel rightPanel;

    private Button downloadButton;

    private Label titleLabel;
    private Label difficultyLabel;
    private Label noteCountLabel;
    private Label rankingLabel;
    private Label durationLabel;

    private RichTextLabel mappersLabel;

    private TextureRect coverImage;
    private TextureRect blurCoverImage;
    private TextureRect downloadIcon;

    private static readonly Texture2D download_icon_texture = GD.Load<Texture2D>("res://user/skins/default/ui/buttons/import.png");
    private static readonly Texture2D delete_icon_texture = GD.Load<Texture2D>("res://user/skins/default/ui/buttons/delete.png");
    private static readonly Texture2D throbber_texture = GD.Load<Texture2D>("res://textures/throbber.png");

    private Tween mapCardTween;
    private Tween downloadButtonTween;

    private StyleBoxFlat downloadButtonStyle;

    private Map downloadedMap;

    private DownloadState state = DownloadState.NotDownloaded;

    private enum DownloadState
    {
        NotDownloaded,
        Downloading,
        Downloaded,
        Failed,
    }

    public override void _Ready()
    {
        mapHolder = GetNode<Control>("Row/Map");
        rightPanelContainer = GetNode<Control>("Row/RightPanel");

        contentContainer = mapHolder.GetNode<MarginContainer>("Content");

        info = mapHolder.GetNode<VBoxContainer>("Content/Info");
        topText = info.GetNode<VBoxContainer>("Top/Text");
        bottomText = info.GetNode<HBoxContainer>("Bottom");

        coverHolder = GetNode<PanelContainer>("Row/CoverHolder");
        blurCoverHolder = mapHolder.GetNode<PanelContainer>("BlurCoverHolder");
        rankingPill = bottomText.GetNode<PanelContainer>("RankingPill");
        notablePill = bottomText.GetNode<PanelContainer>("NotablePill");

        coverDim = coverHolder.GetNode<Panel>("Dim");
        blurCoverDim = blurCoverHolder.GetNode<Panel>("Dim");
        rightPanel = rightPanelContainer.GetNode<Panel>("Panel");
        rightPanelCorners = rightPanel.GetNode<Control>("Corners");

        downloadButton = rightPanel.GetNode<Button>("Download");

        titleLabel = topText.GetNode<Label>("TitleHolder/Title");
        difficultyLabel = topText.GetNode<Label>("Details/VBoxContainer/Difficulty");
        noteCountLabel = topText.GetNode<Label>("Details/VBoxContainer/Notes");
        rankingLabel = rankingPill.GetNode<Label>("Ranking");
        durationLabel = bottomText.GetNode<Label>("Duration");

        mappersLabel = topText.GetNode<RichTextLabel>("Details/VBoxContainer/Mapper");

        coverImage = coverHolder.GetNode<TextureRect>("Cover");
        blurCoverImage = mapHolder.GetNode<TextureRect>("BlurCoverHolder/BlurCover");
        downloadIcon = downloadButton.GetNode<TextureRect>("Icon");

        downloadButtonStyle = (StyleBoxFlat)downloadButton.GetThemeStylebox("normal").Duplicate();

        downloadButton.AddThemeStyleboxOverride("normal", downloadButtonStyle);
        downloadButton.AddThemeStyleboxOverride("hover", downloadButtonStyle);
        downloadButton.AddThemeStyleboxOverride("pressed", downloadButtonStyle);
        downloadButton.AddThemeStyleboxOverride("hover_pressed", downloadButtonStyle);
    }

    public void Bind(JsonElement map, CancellationTokenSource source)
    {
        titleLabel.Text = $"{map.GetProperty("artist").GetString()} - {map.GetProperty("title").GetString()}";
        mappersLabel.Text =
            $"[color=#c8c8c8]by[/color] {string.Join(", ", map.GetProperty("mappers").EnumerateArray().Select(x => x.GetProperty("name").GetString()))}";
        notablePill.Visible = map.GetProperty("mappers").EnumerateArray().Any(x => x.GetProperty("isNotable").GetBoolean());

        int difficulty = map.GetProperty("difficulty").GetInt32();
        string difficultyName = map.GetProperty("difficultyName").GetString();
        string difficultyText = string.IsNullOrEmpty(difficultyName) ? Constants.DIFFICULTIES[difficulty] : difficultyName;

        difficultyLabel.Text = difficultyText;
        difficultyLabel.LabelSettings = (LabelSettings)difficultyLabel.LabelSettings.Duplicate();
        difficultyLabel.LabelSettings.FontColor = Constants.DIFFICULTY_COLORS[difficulty];

        noteCountLabel.Text = $"{map.GetProperty("noteCount").GetInt32().ToString(CultureInfo.InvariantCulture)} notes";

        bool isRanked = map.GetProperty("isRanked").GetBoolean();
        var rankingPillStyle = (StyleBoxFlat)rankingPill.GetThemeStylebox("panel").Duplicate();
        rankingLabel.Text = isRanked ? "RANKED" : "UNRANKED";
        rankingPillStyle.BgColor = isRanked ? Constants.RANKED_COLOR : Constants.UNRANKED_COLOR;
        rankingPill.AddThemeStyleboxOverride("panel", rankingPillStyle);

        var mapLength = TimeSpan.FromMilliseconds(map.GetProperty("length").GetDouble());
        durationLabel.Text =
            mapLength.TotalHours >= 1
                ? mapLength.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                : mapLength.ToString(@"mm\:ss", CultureInfo.InvariantCulture);

        downloadedMap = getDownloadedMap(map.GetProperty("noteHash").GetString());
        state = downloadedMap != null ? DownloadState.Downloaded : DownloadState.NotDownloaded;
        updateDownloadIcon();

        tweenRightPanelColor(rightPanel, rightPanelCorners, state, 0);

        MouseEntered += () =>
        {
            if (state == DownloadState.Downloading)
                return;

            mapCardTween?.Kill();
            mapCardTween = tweenMapCard(this, blurCoverDim, rightPanel, contentContainer, downloadIcon, true);

            downloadButton.SetMouseFilter(MouseFilterEnum.Stop);
        };
        MouseExited += () =>
        {
            if (
                state == DownloadState.Downloading
                || downloadButton.GetGlobalRect().HasPoint(downloadButton.GetGlobalMousePosition())
                || Input.IsMouseButtonPressed(MouseButton.Left)
            )
                return;

            mapCardTween?.Kill();
            mapCardTween = tweenMapCard(this, blurCoverDim, rightPanel, contentContainer, downloadIcon);

            downloadButton.SetMouseFilter(MouseFilterEnum.Ignore);
        };
        downloadButton.MouseEntered += () =>
        {
            if (state == DownloadState.Downloading)
                return;

            downloadButtonTween?.Kill();
            downloadButtonTween = tweenDownloadButton(downloadButton, downloadIcon, downloadButtonStyle, state, true);
        };
        downloadButton.MouseExited += () =>
        {
            if (state == DownloadState.Downloading)
                return;

            downloadButtonTween?.Kill();
            downloadButtonTween = tweenDownloadButton(downloadButton, downloadIcon, downloadButtonStyle, state);

            if (GetGlobalRect().HasPoint(GetGlobalMousePosition()))
                return;

            mapCardTween?.Kill();
            mapCardTween = tweenMapCard(this, blurCoverDim, rightPanel, contentContainer, downloadIcon);

            downloadButton.SetMouseFilter(MouseFilterEnum.Ignore);
        };
        downloadButton.ButtonDown += () =>
        {
            if (state == DownloadState.Downloading)
                return;

            downloadButtonTween?.Kill();
            downloadButtonTween = tweenDownloadButton(downloadButton, downloadIcon, downloadButtonStyle, state, true, true);
        };
        downloadButton.ButtonUp += () =>
        {
            if (state == DownloadState.Downloading)
                return;

            downloadButtonTween?.Kill();
            downloadButtonTween = tweenDownloadButton(downloadButton, downloadIcon, downloadButtonStyle, state, downloadButton.IsHovered());
        };
        downloadButton.Pressed += async () =>
        {
            downloadButton.Disabled = true;

            if (state == DownloadState.Downloaded)
            {
                MapManager.Delete(downloadedMap);
                downloadedMap = null;
                state = DownloadState.NotDownloaded;
            }
            else
            {
                state = DownloadState.Downloading;
                downloadIcon.Texture = throbber_texture;

                Tween throbberTween = downloadIcon.CreateTween().SetLoops();
                throbberTween.TweenProperty(downloadIcon, "rotation", Mathf.Tau, 1).AsRelative();

                downloadedMap = await downloadMap(map.GetProperty("fileUrl"), map.GetProperty("legacyId").GetString(), source);
                state = downloadedMap != null ? DownloadState.Downloaded : DownloadState.Failed;

                throbberTween.Kill();

                downloadIcon.Rotation = 0;
            }

            downloadButtonTween?.Kill();
            downloadButton.Disabled = false;

            bool hovered = downloadButton.IsHovered();

            downloadButtonTween = tweenDownloadButton(downloadButton, downloadIcon, downloadButtonStyle, state, hovered);
            updateDownloadIcon();

            tweenRightPanelColor(rightPanel, rightPanelCorners, state);
        };

        _ = loadCover(map.GetProperty("covers").GetProperty("128"), coverImage, blurCoverImage, source);
    }

    private static async Task<Map> downloadMap(JsonElement fileUrl, string mapId, CancellationTokenSource source)
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
            return null;
        }

        try
        {
            var map = MapParser.PHXM(buffer, mapId);
            MapParser.Encode(map);
            Callable.From(() => MapParser.Instance.EmitSignal(MapParser.SignalName.MapsImportFinished, new[] { map })).CallDeferred();
            _ = ToastNotification.Notify("Map downloaded");
            return map;
        }
        catch (Exception exception)
        {
            await ToastNotification.Notify("Map is corrupted", 2);
            Logger.Error(exception);
            return null;
        }
    }

    private static async Task loadCover(JsonElement coverUrl, TextureRect coverTexture, TextureRect blurCoverTexture, CancellationTokenSource source)
    {
        var token = source.Token;
        Texture2D cover = null;

        try
        {
            if (!token.IsCancellationRequested && coverUrl.ValueKind != JsonValueKind.Null)
            {
                var coverImage = await MapBrowserService.GetCoverImage(coverUrl.GetString(), token);
                cover = ImageTexture.CreateFromImage(coverImage);
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            return;
        }

        if (!(IsInstanceValid(coverTexture) && IsInstanceValid(blurCoverTexture)))
            return;

        coverTexture.Texture = cover;
        blurCoverTexture.Texture = cover;
    }

    private static Map getDownloadedMap(string hash)
    {
        return MapManager.Maps.FirstOrDefault(m => m.ObjectHash == hash);
    }

    private void updateDownloadIcon()
    {
        downloadIcon.PivotOffset = downloadIcon.Size / 2;
        downloadIcon.Texture = state == DownloadState.Downloaded ? delete_icon_texture : download_icon_texture;

        if (state == DownloadState.Downloaded)
        {
            downloadIcon.Texture = delete_icon_texture;
            downloadIcon.SelfModulate = Color.Color8(57, 66, 70);
        }
        else
        {
            downloadIcon.Texture = download_icon_texture;
            downloadIcon.SelfModulate = Color.Color8(255, 255, 255);
        }
    }

    private static Tween tweenMapCard(
        PanelContainer mapCard,
        Panel blurCoverDim,
        Panel rightPanel,
        MarginContainer contentContainer,
        TextureRect downloadIcon,
        bool hover = false,
        double duration = 0.2
    )
    {
        Tween mapCardTween = mapCard.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut).SetParallel();
        mapCardTween.TweenProperty(blurCoverDim, "modulate", Color.Color8(255, 255, 255, (byte)(hover ? 64 : 0)), duration);
        mapCardTween.TweenProperty(rightPanel, "custom_minimum_size", new Vector2(hover ? 40 : 15, 0), duration);
        mapCardTween.TweenProperty(contentContainer, "theme_override_constants/margin_right", hover ? 40 : 15, duration);
        mapCardTween.TweenProperty(downloadIcon, "modulate", Color.Color8(255, 255, 255, (byte)(hover ? 204 : 0)), duration);

        return mapCardTween;
    }

    private static Tween tweenRightPanelColor(Panel rightPanel, Control rightPanelCorners, DownloadState state, double duration = 0.2)
    {
        Color color = state == DownloadState.Downloaded ? Constants.DOWNLOADED_COLOR : Color.Color8(57, 66, 70);

        Tween rightPanelTween = rightPanel.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut).SetParallel();
        rightPanelTween.TweenProperty(rightPanel, "self_modulate", color, duration);
        rightPanelTween.TweenProperty(rightPanelCorners, "modulate", color, duration);

        return rightPanelTween;
    }

    private static Tween tweenDownloadButton(
        Button downloadButton,
        TextureRect downloadIcon,
        StyleBoxFlat downloadButtonStyle,
        DownloadState state,
        bool hover = false,
        bool pressed = false,
        double duration = 0.2
    )
    {
        bool downloaded = state == DownloadState.Downloaded;

        Tween downloadButtonTween = downloadButton.CreateTween().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut).SetParallel();
        downloadButtonTween.TweenProperty(
            downloadIcon,
            "modulate",
            Color.Color8(255, 255, 255, downloaded ? (byte)255 : (byte)(hover ? 255 : 204)),
            duration
        );
        downloadButtonTween.TweenProperty(
            downloadButtonStyle,
            "bg_color",
            Color.Color8(
                255,
                255,
                255,
                (byte)(
                    pressed ? (downloaded ? 90 : 70)
                    : hover ? (downloaded ? 85 : 40)
                    : 0
                )
            ),
            duration
        );
        return downloadButtonTween;
    }
}
