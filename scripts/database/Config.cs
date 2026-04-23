
/// <summary>
/// Config for startup
/// </summary>
public class Config
{
    public static string DEFAULT_ID = "_CONFIG";

    public static Config Instance { get; set; } = new Config();

    /// <summary>
    /// ID of the selected <see cref="SettingsProfile"/>
    /// </summary>
    public string SettingsProfileID { get; set; }

    private Config init()
    {
        return new Config();
    }
}
