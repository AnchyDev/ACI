using ACI.Server.Config;
using ACI.Server.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ACI.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);

            host.ConfigureLogging(logging =>
            {
                logging.AddConsole();
            });

            host.ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("config.json");
            });

            host.ConfigureServices(services =>
            {
                services.AddSingleton<DiscordService>();
                services.AddSingleton<InterconnectService>();

                services.AddHostedService<DiscordBackgroundService>();
                services.AddHostedService<InterconnectBackgroundService>();

                services.AddSingleton<ACIConfig>();
            });

            var builder = host.Build();

            var config = builder.Services.GetService<IConfiguration>();
            var myConfig = builder.Services.GetService<ACIConfig>();
            if (config is not null && myConfig is not null)
            {
                config.Bind(myConfig);
            }

            await builder.RunAsync();
            await builder.WaitForShutdownAsync();
        }
    }
}
