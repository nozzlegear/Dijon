using System.Threading.Tasks;
using Discord.Commands;

namespace Dijon.Modules
{
    [Group("role")]
    public class RoleModule : ModuleBase
    {
        [Command("add"), Summary("Adds a user to a role")]
        public async Task AddToRole()
        {
            await ReplyAsync("AddToRole Not yet implemented");
        }

        [Command("remove"), Summary("Removes a user from a role")]
        public async Task RemoveFromRole()
        {
            await ReplyAsync("RemoveFromRole Not yet implemented");
        }

        [Command("rename"), Summary("Renames a role")]
        public async Task RenameRole()
        {
            await ReplyAsync("RenameRole Not yet implemented");
        }

        [Command("list"), Summary("Lists all users in a role")]
        public async Task ListRoles()
        {
            await ReplyAsync("ListRoles Not yet implemented");
        }

        [Command("create"), Summary("Creates a new role")]
        public async Task CreateRole()
        {
            await ReplyAsync("CreateRole Not yet implemented");
        }

        [Command("delete"), Summary("Deletes an entire role, removing all users from it")]
        public async Task DeleteRole()
        {
            await ReplyAsync("DeleteRole Not yet implemented");
        }
    }
}