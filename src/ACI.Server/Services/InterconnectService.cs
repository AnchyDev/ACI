using ACI.Server.Config;
using ACI.Server.Network;
using ACI.Server.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ACI.Server.Services
{
    public class InterconnectService : BackgroundService
    {
        public bool Ready { get; set; }

        private TcpListener listener;

        private readonly ILogger<InterconnectService> logger;
        private readonly ACIConfig config;
        private readonly IServiceProvider serviceProvider;

        private List<TcpClient> connectedClients = new();
        private Dictionary<ACIOpCode, Func<TcpClient, NetworkStream, Task>> opCodes;

        public InterconnectService(ILogger<InterconnectService> logger, ACIConfig config, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.config = config;
            this.serviceProvider = serviceProvider;

            Debug.Assert(config is not null);
            Debug.Assert(!string.IsNullOrEmpty(config.IPAddress));

            listener = new TcpListener(IPAddress.Parse(config.IPAddress), config.Port);
            opCodes = new()
            {
                { ACIOpCode.CMSG_MSG, HandleMessageOpCode }
            };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting listener..");
            listener.Start();

            logger.LogInformation("Waiting for connections..");
            Ready = true;
            await StartListeningAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                foreach (var client in connectedClients)
                {
                    client.Close();
                }

                listener.Stop();
            });

            Ready = false;
        }

        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClient(client);
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();

                connectedClients.Add(client);
                logger.LogInformation($"Client '{client.Client.RemoteEndPoint}' connected.");

                while (true)
                {
                    await HandlePayloads(client);
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"An exception occured for the client '{client.Client.RemoteEndPoint}': {ex.Message}");
                connectedClients.Remove(client);
                return;
            }
        }

        private async Task HandlePayloads(TcpClient client)
        {
            var stream = client.GetStream();

            var opCodeBuf = new byte[sizeof(uint)];
            int bytesRead = await stream.ReadAsync(opCodeBuf);

            if (bytesRead == 0)
            {
                return;
            }

            var opCode = BitConverter.ToUInt32(opCodeBuf);

            if (!opCodes.ContainsKey((ACIOpCode)opCode))
            {
                logger.LogInformation($"Got invalid OpCode '{opCode}' from client '{client.Client.RemoteEndPoint}'.");
                return;
            }

            logger.LogInformation($"Received OpCode '{(ACIOpCode)opCode}'.");

            // Run handler
            await opCodes[(ACIOpCode)opCode](client, stream);
        }

        public async Task HandleMessageOpCode(TcpClient client, NetworkStream stream)
        {
            string json = await stream.ReadStringAsync();
            var packet = JsonSerializer.Deserialize<PacketMessage>(json);

            if (packet is null)
            {
                logger.LogInformation("Failed to deserialize packet.");
                return;
            }

            logger.LogInformation($"Received message: [{packet.Origin}][{packet.Author}]: {packet.Message}");

            using (var scope = serviceProvider.CreateScope())
            {
                var discord = scope.ServiceProvider.GetRequiredService<DiscordService>();
                if (discord is null)
                {
                    logger.LogError("Failed to get the DiscordService while handling Server message, service was null.");
                    return;
                }

                if (!discord.Ready)
                {
                    logger.LogWarning("DiscordService was not ready, ignoring message.");
                    return;
                }

                await discord.BroadcastAsync(packet);
            }

            logger.LogInformation("Broadcasting message to connected clients.");
            foreach (var connectedClient in connectedClients)
            {
                // Don't broadcast back to the original sender.
                if (client == connectedClient)
                {
                    continue;
                }

                logger.LogInformation($"Sending message to {connectedClient.Client.RemoteEndPoint}");
                await connectedClient.GetStream().WriteAsync(BitConverter.GetBytes((uint)ACIOpCode.SMSG_MSG));

                await connectedClient.GetStream().WriteAsync(BitConverter.GetBytes(json.Length));
                await connectedClient.GetStream().WriteAsync(Encoding.UTF8.GetBytes(json));
            }
        }

        public async Task BroadcastAsync(string json)
        {
            foreach(var client in connectedClients)
            {
                await client.GetStream().WriteAsync(BitConverter.GetBytes((uint)ACIOpCode.SMSG_MSG));
                await client.GetStream().WriteAsync(BitConverter.GetBytes(json.Length));
                await client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(json));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) { }
    }
}
