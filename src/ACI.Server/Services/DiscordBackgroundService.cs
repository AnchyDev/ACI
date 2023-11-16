using Microsoft.Extensions.Hosting;

namespace ACI.Server.Services
{
    public class DiscordBackgroundService : BackgroundService
    {
        private readonly DiscordService discord;

        public DiscordBackgroundService(DiscordService discord)
        {
            this.discord = discord;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await discord.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await discord.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) { }
    }
}
