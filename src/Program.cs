using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Discord.Net;
using Discord.WebSocket;
using Discord;
using Dijon.Environment;
using Discord.Commands;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Dijon
{
    public class Program
    {
        DiscordSocketClient Client;
        CommandService Commands;
        IServiceProvider Service;

        public static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            var tokenVar = new EnvironmentVariable("DIJON_BOT_TOKEN");
            var clientIdVar = new EnvironmentVariable("DIJON_CLIENT_ID");

            if (!tokenVar.HasValue)
            {
                throw new NullReferenceException($"Required {nameof(tokenVar)} value is null or empty.");
            }

            if (!clientIdVar.HasValue)
            {
                throw new NullReferenceException($"Required {nameof(clientIdVar)} value is null or empty.");
            }

            Client = new DiscordSocketClient();
            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = true
            });
            Service = new ServiceCollection().BuildServiceProvider();

            await InstallCommands();
            await Client.LoginAsync(TokenType.Bot, tokenVar.Value);
            await Client.StartAsync();

            await Task.Delay(-1);

            // CreateWebHostBuilder(args).Build().Run();
        }

        public async Task InstallCommands()
        {
            Client.Log += message =>
            {
                Console.WriteLine("Message received {0}", message.Message);

                return Task.CompletedTask;
            };

            Client.MessageReceived += HandleMessage;

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleMessage(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;

            if (message == null) return;

            int argPosition = 0;

            if (!message.HasStringPrefix("!dijon ", ref argPosition, StringComparison.OrdinalIgnoreCase) && !message.HasMentionPrefix(Client.CurrentUser, ref argPosition))
            {
                return;
            }

            var context = new CommandContext(Client, message);
            var result = await Commands.ExecuteAsync(context, argPosition, Service);

            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        // public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //     WebHost.CreateDefaultBuilder(args)
        //         .UseStartup<Startup>();
    }
}
