public class QueryParameters
{
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
}

public class MapQueryParameters : QueryParameters
{
    public string Query { get; set; }
    public string Mapper { get; set; }
    public string Artist { get; set; }
}
