using System.Text.Json.Serialization;

namespace ACI.Server.Config
{
    public class ACIDiscordConfig
    {
        public string? BotToken { get; set; }
        public List<ACIDiscordServerConfig>? Servers { get; set; }
    }
}
