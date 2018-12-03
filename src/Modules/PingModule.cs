using System.Threading.Tasks;
using Discord.Commands;

namespace Dijon.Modules
{
    public class PingModule : ModuleBase
    {
        [Command("ping"), Summary("Pings and pongs")]
        public async Task Ping()
        {
            await ReplyAsync("Pong!");
        }
    }
}