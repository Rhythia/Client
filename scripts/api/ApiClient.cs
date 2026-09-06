using System;
using System.Net.Http;

public static class ApiClient
{
    public static readonly HttpClient CLIENT = new()
    {
        BaseAddress = new Uri("https://rhythia-api.nyarw.moe")
    };
}
