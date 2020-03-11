using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer
{
    class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private readonly FirestoreDb _database;
        private readonly CollectionReference _players;

        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider,
            FirestoreDb database)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _database = database;

            // event subscription
            _discord.MessageReceived += OnMessageRecievedAsync;
            _discord.GuildAvailable += CreateDemoLeaderboardsAsync;
#if !DEBUG
            _discord.JoinedGuild += OnJoinedGuildAsync;
#endif
        }

        private async Task OnMessageRecievedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg is null || msg.Author.IsBot) return;

            var context = new SocketCommandContext(_discord, msg);

            int argPos = 0;
            bool isCommand = msg.HasStringPrefix(_config["prefix"], ref argPos) ||
                             msg.HasMentionPrefix(_discord.CurrentUser, ref argPos);

            bool isSuccess = false;

            var enoughTimePassed = await GetEnoughTimePassedAsync(context.User.Id, context.Message.Timestamp);

            if (isCommand && enoughTimePassed)
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                if (!result.IsSuccess)
                {
                    Emoji errorEmoji = null;

                    switch (result.Error)
                    {
                        case CommandError.UnknownCommand:
                            errorEmoji = new Emoji("❓");
                            break;
                        case CommandError.ParseFailed:
                        case CommandError.Exception:
                        case CommandError.Unsuccessful:
                            errorEmoji = new Emoji("⚡");
                            break;
                        case CommandError.BadArgCount:
                            errorEmoji = new Emoji("❌");
                            break;
                        case CommandError.UnmetPrecondition:
                            errorEmoji = null;
                            break;
                        default:
                            await context.Channel.SendMessageAsync(result.ErrorReason);
                            break;
                    }

                    if (errorEmoji != null)
                        await context.Message.AddReactionAsync(errorEmoji);

                    Console.WriteLine
                        ($"[{DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss")}] " +
                        $"{context.Guild.Name} in {context.Channel.Name} " +
                        $"from {context.User.Username}: {result.ErrorReason}");
                }

                isSuccess = result.IsSuccess;
            }

            else if (isCommand && !enoughTimePassed)
            {
                await context.Message.AddReactionAsync(Bot.TimeEmote);
            }

            Player player = await Player.GetAsync(msg.Author.Id, _database.Collection("players"));

            DbMessage mostRecentMsg = player.RecentMessages.Count > 0 ? player.RecentMessages.Aggregate((l, r) => l.CreateTime > r.CreateTime ? l : r) : null;

            if (player.RecentMessages.TrueForAll(m => m.Content != msg.Content) && enoughTimePassed)
                await player.AddBoostAsync(context);

            await player.LogMessageAsync(msg, isCommand && isSuccess);
        }

        private async Task CreateDemoLeaderboardsAsync(SocketGuild guild)
        {
            ITextChannel channel = guild.TextChannels.Where(x => x.Name == "demo-battleground").FirstOrDefault();

            if (channel is null)
            {
                channel = await guild.CreateTextChannelAsync("demo-battleground");
                await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(_discord.CurrentUser, new OverwritePermissions(sendMessages: PermValue.Allow));
            }

            Bot.demoLeaderboard = await Leaderboard.CreateAsync(LeaderboardType.Demo, 5, channel, _players);
            Bot.KdrLeaderboard = await Leaderboard.CreateAsync(LeaderboardType.KDR, 5, channel, _players);
        }

        private async Task OnJoinedGuildAsync(SocketGuild guild)
        {
            if(guild.Id != 300815426462679051) await guild.LeaveAsync();
        }

        private async Task<bool> GetEnoughTimePassedAsync(ulong userId, DateTimeOffset msgTimestamp)
        {
            List<DocumentReference> recentMsgs = await _database.Collection($"players/{userId}/recent-messages").ListDocumentsAsync().ToList();
            DocumentReference mostRecentMsg = recentMsgs.Count > 0 ? recentMsgs.ElementAt(0) : null;

            if (mostRecentMsg is null) return true;

            else foreach (var msg in recentMsgs)
            {
                if ((await msg.GetSnapshotAsync()).GetValue<DateTimeOffset>("create-time") < (await mostRecentMsg.GetSnapshotAsync()).GetValue<DateTimeOffset>("create-time"))
                    mostRecentMsg = msg;
            }

            var createTime = (await mostRecentMsg.GetSnapshotAsync()).GetValue<DateTimeOffset>("create-time");

            return msgTimestamp - createTime > TimeSpan.FromSeconds(1.0);
        }
    }
}
