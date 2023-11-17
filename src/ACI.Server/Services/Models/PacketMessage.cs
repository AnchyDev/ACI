using System.Text.Json.Serialization;

namespace ACI.Server.Services.Models
{
    public class PacketMessage
    {
        [JsonPropertyName("origin")]
        public string? Origin { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("faction")]
        public Faction Faction { get; set; }
    }
}
