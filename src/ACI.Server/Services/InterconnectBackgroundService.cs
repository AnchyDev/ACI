using Microsoft.Extensions.Hosting;

namespace ACI.Server.Services
{
    internal class InterconnectBackgroundService : BackgroundService
    {
        private readonly InterconnectService interconnect;

        public InterconnectBackgroundService(InterconnectService interconnect)
        {
            this.interconnect = interconnect;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await interconnect.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await interconnect.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) { }
    }
}
