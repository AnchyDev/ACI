using System.Text.Json.Serialization;

namespace ACI.Server.Config
{
    public class ACIConfig
    {
        public string? IPAddress { get; set; }
        public int Port { get; set; }
        public string? PrivateKey { get; set; }
        public ACIDiscordConfig? Discord { get; set; }
    }
}
