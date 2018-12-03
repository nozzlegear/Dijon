using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Dijon.Models;
using Dijon.Config;

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
            BotConfig configuration = Configuration.ReadConfig();

            Client = new DiscordSocketClient();
            Commands = new CommandService(new CommandServiceConfig
            {
                CaseSensitiveCommands = true
            });
            Service = new ServiceCollection().BuildServiceProvider();

            await InstallCommands();
            await Client.LoginAsync(TokenType.Bot, configuration.botToken);
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
