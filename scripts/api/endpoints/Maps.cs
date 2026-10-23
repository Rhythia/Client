using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

public static class Maps
{
    public static async Task<JsonDocument> Search(MapQueryParameters queryParameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["limit"] = queryParameters.Limit.ToString(CultureInfo.InvariantCulture);
        query["offset"] = queryParameters.Offset.ToString(CultureInfo.InvariantCulture);
        query["sortBy"] = queryParameters.SortBy;
        query["sortDirection"] = queryParameters.SortDirection;

        if (!string.IsNullOrEmpty(queryParameters.Query))
            query["query"] = queryParameters.Query;
        if (!string.IsNullOrEmpty(queryParameters.Mapper))
            query["mapper"] = queryParameters.Mapper;
        if (!string.IsNullOrEmpty(queryParameters.Artist))
            query["artist"] = queryParameters.Artist;
        if (queryParameters.Difficulties.Length > 0)
            query["difficulty"] = string.Join(",", queryParameters.Difficulties);
        if (queryParameters.Status.Length > 0)
            query["status"] = string.Join(",", queryParameters.Status);

        string url = $"/maps?{query}";

        var response = await ApiClient.CLIENT.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }
}
