namespace EveRAT.Models;

public class BotSettings
{
    public string BotToken { get; set; } = "Discord_bot_token";
    public string ClientId { get; set; } = "Eve_app_clientId";
    public string SecretKey { get; set; } = "Eve_app_securityKey";
    public string CallbackUrl { get; set; } = "Eve_app_callbackUrl";
    public string UserAgent { get; set; } = "Eve_app_userAgent";
    public ulong DiscordServerId { get; set; } = 0;
    public ulong DiscordChannelId { get; set; } = 0;
    public int[] Limits { get; set; } = { 300, 500 };
    public int RefreshEvery { get; set; } = 5;
    public int DaysToKeepHistory { get; set; } = 20;
    public bool ActivateStats { get; set; } = false;
    public List<Tuple<int, string>> Systems { get; set; } = new();
}