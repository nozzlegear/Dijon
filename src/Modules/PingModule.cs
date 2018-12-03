using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Dijon.Modules
{
    public class PingModule : ModuleBase
    {
        [Command("ping"), Summary("Pings and pongs")]
        public async Task Ping()
        {

            EmbedBuilder builder = new EmbedBuilder();

            builder.Title = ":ping_pong: Pong!";
            builder.Description = Environment.NewLine + ":heartbeat: **" + (Context.Client as DiscordSocketClient).Latency + " ms** heartbeat latency";
            builder.Color = Color.Green;

            await ReplyAsync("", false, builder);
        }
    }
}