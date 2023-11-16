using System.Text.Json.Serialization;

namespace ACI.Server.Config
{
    public class ACIDiscordServerConfig
    {
        public string? Name { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
    }
}
