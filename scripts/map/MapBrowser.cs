using System;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

public static class MapBrowser
{
    public static async Task<JsonElement[]> Search(MapQueryParameters queryParameters)
    {
        var result = await Maps.Search(queryParameters);
        return [.. result.RootElement.GetProperty("maps").EnumerateArray()];
    }

    public static async Task<Image> GetCoverImage(string coverUrl)
    {
        var coverUri = new Uri(ApiClient.CLIENT.BaseAddress, coverUrl);
        byte[] coverBytes = await ApiClient.CLIENT.GetByteArrayAsync(coverUri);

        var image = new Image();
        image.LoadPngFromBuffer(coverBytes);

        return image.IsEmpty() ? null : image;
    }

    public static async Task<byte[]> GetAudioBytes(string audioUrl)
    {
        var audioUri = new Uri(ApiClient.CLIENT.BaseAddress, audioUrl);
        return await ApiClient.CLIENT.GetByteArrayAsync(audioUri);
    }
}
