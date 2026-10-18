using DiscordRPC;

public class Discord
{
    private static string appId = "1231699688340590722";
    public static DiscordRpcClient Client { get; private set; }

    static Discord()
    {
        // TODO: Add logging here
        Client = new DiscordRpcClient(appId) { };

        Client.Initialize();

        Client.SetPresence(new RichPresence() { Assets = new Assets() { LargeImageKey = "short" } });
    }
}
