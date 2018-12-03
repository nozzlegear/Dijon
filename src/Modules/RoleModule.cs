using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Dijon.Modules
{
    [Group("role")]
    public class RoleModule : ModuleBase
    {
        static Dictionary<string, List<string>> Roles { get; } = new Dictionary<string, List<string>>();

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
        public async Task CreateRole([Summary("The name of the role to create")] string roleName)
        {
            roleName = roleName?.Trim() ?? "";

            if (Roles.ContainsKey(roleName))
            {
                var users = Roles.GetValueOrDefault(roleName);

                await ReplyAsync($"Role already exists, {users.Count} are currently assigned to {roleName}.");

                return;
            }

            Roles.Add(roleName, new List<string>());

            await ReplyAsync($"Created role {roleName}.");
        }

        [Command("delete"), Summary("Deletes an entire role, removing all users from it")]
        public async Task DeleteRole()
        {
            await ReplyAsync("DeleteRole Not yet implemented");
        }
    }
}