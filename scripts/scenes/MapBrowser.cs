using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public partial class MapBrowser : Control
{
    private Panel holder;
    private ScrollContainer results;
    private PanelContainer mapCardTemplate;

    public override void _Ready()
    {
        holder = GetNode<Panel>("Holder");
        results = holder.GetNode<ScrollContainer>("Background/Layout/Results");
        mapCardTemplate = results.GetNode<PanelContainer>("RowsMargin/Rows/MapCardTemplate");

        mapCardTemplate.Visible = false;

        _ = populate();
    }

    private async Task populate()
    {
        JsonElement[] maps = await MapBrowserService.Search(new MapQueryParameters());

        var tasks = maps.Select(async map =>
        {
            JsonElement coverUrl = map.GetProperty("coverUrl");
            Texture2D cover = null;

            GD.Print($"{map.GetProperty("title").GetString()}: {coverUrl.ToString()}");

            if (coverUrl.ValueKind != JsonValueKind.Null)
            {
                cover = ImageTexture.CreateFromImage(await MapBrowserService.GetCoverImage(coverUrl.ToString()));
            }

            return (Map: map, Cover: cover);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (map, cover) in results)
        {
            if (mapCardTemplate.Duplicate() is not PanelContainer mapCard) { continue; }

            mapCard.Visible = true;
            mapCardTemplate.GetParent().AddChild(mapCard);

            TextureRect coverImage = mapCard.GetNode<TextureRect>("Row/CoverHolder/Cover");
            TextureRect blurCoverImage = mapCard.GetNode<TextureRect>("Row/Map/BlurCoverHolder/BlurCover");

            VBoxContainer info = mapCard.GetNode<VBoxContainer>("Row/Map/Content/Info");
            Label name = info.GetNode<Label>("Top/Text/Title");
            Label mappersLabel = info.GetNode<Label>("Top/Text/Mapper");
            Label difficultyName = info.GetNode<Label>("Top/Text/Difficulty");
            Label noteCountLabel = info.GetNode<Label>("Bottom/Ranking/Notes");
            Label rankedLabel = info.GetNode<Label>("Bottom/Ranking/Ranking");
            Label durationLabel = info.GetNode<Label>("Bottom/Duration");

            coverImage.Texture = cover;
            blurCoverImage.Texture = cover;

            name.Text = $"{map.GetProperty("artist").GetString()} - {map.GetProperty("title")}";
            mappersLabel.Text = $"mapped by {string.Join(", ", map.GetProperty("mappers").EnumerateArray().Select(x => x.GetProperty("name").GetString()))}";
            difficultyName.Text = map.GetProperty("difficultyName").GetString();
            noteCountLabel.Text = map.GetProperty("noteCount").GetInt32().ToString();
            rankedLabel.Text = (map.GetProperty("isRanked").GetBoolean()) ? "RANKED" : "UNRANKED";

            var duration = TimeSpan.FromMilliseconds(map.GetProperty("length").GetDouble());

            durationLabel.Text = duration.TotalHours >= 1
                ? duration.ToString(@"hh\:mm\:ss")
                : duration.ToString(@"mm\:ss");
        }
    }
}
