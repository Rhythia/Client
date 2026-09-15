using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class MapBrowser : Control
{
    public static MapBrowser Instance;

    public bool Shown = false;

    private Button hideButton;
    private Panel holder;
    private ScrollContainer results;
    private PanelContainer mapCardTemplate;

    public override void _Ready()
    {
        Instance = this;

        hideButton = GetNode<Button>("Hide");
        holder = GetNode<Panel>("Holder");
        results = holder.GetNode<ScrollContainer>("Background/Layout/Results");
        mapCardTemplate = results.GetNode<PanelContainer>("RowsMargin/Rows/MapCardTemplate");

        mapCardTemplate.Visible = false;

        Shown = false;
        Visible = false;

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
            _ = populate();
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

    private async Task populate()
    {
        JsonElement[] maps = await MapBrowserService.Search(new MapQueryParameters());

        foreach (var map in maps)
        {
            if (mapCardTemplate.Duplicate() is not PanelContainer mapCard)
            {
                continue;
            }

            mapCard.Visible = true;
            mapCardTemplate.GetParent().AddChild(mapCard);

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

            _ = loadCover(map.GetProperty("covers").GetProperty("128"), coverImage, blurCoverImage);

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

    private static async Task loadCover(JsonElement coverUrl, TextureRect coverTexture, TextureRect blurCoverTexture)
    {
        Texture2D cover = null;

        if (coverUrl.ValueKind != JsonValueKind.Null)
        {
            var coverImage = await MapBrowserService.GetCoverImage(coverUrl.GetString());
            cover = ImageTexture.CreateFromImage(coverImage);
        }

        coverTexture.Texture = cover;
        blurCoverTexture.Texture = cover;
    }

    private void clearResults()
    {
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
