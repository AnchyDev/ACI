using ACI.Server.Config;
using ACI.Server.Services.Models;

using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace ACI.Server.Services
{
    public class DiscordService : IHostedService
    {
        public bool Ready { get; set; }

        private DiscordSocketClient discord;

        private readonly ILogger<DiscordService> logger;
        private readonly ACIConfig config;
        private readonly IServiceProvider serviceProvider;

        public DiscordService(ILogger<DiscordService> logger, ACIConfig config, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.config = config;
            this.serviceProvider = serviceProvider;

            discord = new DiscordSocketClient(new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });

            discord.MessageReceived += Discord_MessageReceived;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(config is not null);
            Debug.Assert(config.Discord is not null);
            Debug.Assert(!string.IsNullOrEmpty(config.Discord.BotToken));

            logger.LogInformation("Logging in Discord bot..");
            await discord.LoginAsync(TokenType.Bot, config.Discord.BotToken);

            logger.LogInformation("Logged in, starting bot service..");
            await discord.StartAsync();
            Ready = true;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if(discord.LoginState == LoginState.LoggedIn)
            {
                await discord.LogoutAsync();
            }

            await discord.DisposeAsync();
            Ready = false;
        }

        private async Task Discord_MessageReceived(SocketMessage arg)
        {
            // Ignore bots
            if(arg.Author.IsBot)
            {
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var interconnect = scope.ServiceProvider.GetRequiredService<InterconnectService>();
                if(interconnect is null)
                {
                    logger.LogError("Failed to get the InterconnectService while handling Discord message, service was null");
                    return;
                }

                if (!interconnect.Ready)
                {
                    return;
                }

                string json = "{ \"origin\": \"Discord\", \"author\": \"" + arg.Author.Username + "\", \"message\":\"" + arg.CleanContent + "\" }";
                await interconnect.BroadcastAsync(json);
                await BroadcastAsync(new PacketMessage()
                {
                    Origin = "Discord",
                    Author = arg.Author.Username,
                    Message = arg.CleanContent
                }, arg.Channel.Id);
            }
        }

        public async Task BroadcastAsync(PacketMessage message, ulong? originChannel = null)
        {
            Debug.Assert(discord is not null);
            Debug.Assert(config is not null);
            Debug.Assert(config.Discord is not null);
            Debug.Assert(config.Discord.Servers is not null);

            foreach (var server in config.Discord.Servers)
            {
                var guild = discord.GetGuild(server.GuildId);
                if (guild is null)
                {
                    logger.LogError($"Failed to find Guild for server {server.Name}.");
                    continue;
                }

                // Don't broadcast discord messages back to the same channel.
                if(originChannel is not null)
                {
                    if(server.ChannelId == originChannel.Value)
                    {
                        continue;
                    }
                }

                var channel = guild.GetTextChannel(server.ChannelId);
                if (channel is null)
                {
                    logger.LogError($"Failed to find TextChannel for server {server.Name}");
                    continue;
                }

                string icon = "https://i.imgur.com/wYuzaEL.png";

                if (message.Origin != "Discord")
                {
                    icon = message.Faction == Faction.Alliance ? "https://i.imgur.com/32rFlXr.png" : "https://i.imgur.com/RMxJKjF.png";
                }

                var builder = new EmbedBuilder()
                    .WithAuthor($"{message.Origin} - {message.Author}", icon)
                    .WithDescription(message.Message)
                    .WithCurrentTimestamp()
                    .WithColor(Color.Green);

                await channel.SendMessageAsync(embed: builder.Build());
            }
        }
    }
}
