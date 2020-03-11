using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer
{
    class RequireRoleAttribute : RequireContextAttribute
    {
        private readonly ulong _roleId;

        public RequireRoleAttribute(ulong roleId) : base(ContextType.Guild)
            => _roleId = roleId;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var allowedRoleIds = new List<ulong>();

            if (_roleId != 0)
            {
                IRole guildRole;

                try { guildRole = context.Guild.Roles.Where(x => x.Id == _roleId).First(); }
                catch (InvalidOperationException) { return Task.FromResult(PreconditionResult.FromError($"Required role ID {_roleId} not found in guild {context.Guild.Name}.")); }

                allowedRoleIds.AddRange(context.Guild.Roles.Where(x => x.Position >= guildRole.Position).Select(x => x.Id));
            }

            return (context.User as IGuildUser).RoleIds.Intersect(allowedRoleIds).Any()
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("You do not have a role required to execute this command."));
        }
    }
}
