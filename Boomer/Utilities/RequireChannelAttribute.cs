using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer
{
    class RequireChannelAttribute : RequireContextAttribute
    {
        private readonly ulong _channelId;

        private readonly ulong[] _channelIds;

        public RequireChannelAttribute(ulong channelId) : base(ContextType.Guild)
            => _channelId = channelId;

        public RequireChannelAttribute(ulong[] channelIds) : base(ContextType.Guild)
            => _channelIds = channelIds;

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var allowedChannelIds = new List<ulong>();

            if (_channelId != 0 || _channelIds.Count() != 0)
            {
                var testIds = new List<ulong>();
                if (_channelId != 0) testIds.Add(_channelId);
                if (_channelIds.Count() != 0) testIds.AddRange(_channelIds);

                foreach (var id in testIds)
                {
                    IChannel channel;

                    try { channel = (await context.Guild.GetTextChannelsAsync()).Where(x => x.Id == id).First(); }
                    catch (InvalidOperationException) { return await Task.FromResult(PreconditionResult.FromError($"Required channel ID {id} not found in {context.Guild.Name}.")); }

                    allowedChannelIds.Add(channel.Id);
                }
            }

            return allowedChannelIds.Any(x => x == context.Channel.Id) ?
                await Task.FromResult(PreconditionResult.FromSuccess()) :
                await Task.FromResult(PreconditionResult.FromError("You're not in a channel required to execute this command."));
        }
    }
}
