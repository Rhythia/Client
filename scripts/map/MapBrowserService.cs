using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;

public static class MapBrowserService
{
    public static async Task<JsonElement[]> Search(MapQueryParameters queryParameters)
    {
        using var result = await Maps.Search(queryParameters);
        return [.. result.RootElement.GetProperty("maps").EnumerateArray().Select(e => e.Clone())];
    }

    public static async Task<byte[]> GetMapFile(string fileUrl, CancellationToken token)
    {
        var fileUri = new Uri(ApiClient.CLIENT.BaseAddress, fileUrl);
        return await ApiClient.CLIENT.GetByteArrayAsync(fileUri, token);
    }

    public static async Task<Image> GetCoverImage(string coverUrl, CancellationToken token)
    {
        var coverUri = new Uri(ApiClient.CLIENT.BaseAddress, coverUrl);
        byte[] coverBytes = await ApiClient.CLIENT.GetByteArrayAsync(coverUri, token);

        var image = new Image();
        image.LoadWebpFromBuffer(coverBytes);

        return image.IsEmpty() ? null : image;
    }

    public static async Task<byte[]> GetAudioBytes(string audioUrl, CancellationToken token)
    {
        var audioUri = new Uri(ApiClient.CLIENT.BaseAddress, audioUrl);
        return await ApiClient.CLIENT.GetByteArrayAsync(audioUri, token);
    }
}
