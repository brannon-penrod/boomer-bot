using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer.Modules
{

#if !DEBUG
        [RequireChannel(new ulong[]{
            594586575254323210, //#demo-battleground,
            484792885334507531 //#hypos-bot-control
        })]
#endif
    public class ChatDemo : ModuleBase<SocketCommandContext>
    {
        private readonly FirestoreDb _database;
        private readonly CollectionReference _playerColl;
        private readonly CollectionReference _demoMsgs;
        private readonly CollectionReference _missMsgs;
        private CollectionReference _leaderboards;

        public ChatDemo(FirestoreDb database)
        {
            _database = database;
            _playerColl = database.Collection("players");
            _missMsgs = database.Collection("misc-data/response-msgs/miss-msgs");
            _demoMsgs = database.Collection("misc-data/response-msgs/demo-msgs");
            _leaderboards = database.Collection("leaderboards");
        }

        [Command("boost")]
        [Summary("Checks your current boost count")]
        public async Task CheckBoostAsync()
        {
            Player player = await Player.GetAsync(Context.User.Id, _playerColl);

            await ReplyAsync(embed: new EmbedBuilder()
            {
                Color = Bot.SuccessColor,
                Description = $"You have {player.Data.Boost} boost."

            }.Build());
        }

        [Command("demo")]
        [Summary("Attempt to demo target player.")]
        public async Task DemoAsync([Remainder] SocketGuildUser target)
        {
            if (target == Context.User as SocketGuildUser)
            {
                await ReplyAsync(embed: Bot.ErrorEmbed("You can't target yourself!"));
                return;
            }

            if (target.IsBot)
            {
                await ReplyAsync(embed: Bot.ErrorEmbed("You can't target bots >:("));
                return;
            }

            Player contextPlayer = await Player.GetAsync(Context.User.Id, _playerColl);
            Player targetPlayer = await Player.GetAsync(target.Id, _playerColl);

            var (enoughBoost, targetWasDemoed, boostUsed) =
                await contextPlayer.AttemptDemoAsync(targetPlayer);

            if (!enoughBoost)
            {
                await ReplyAsync(embed: Bot.ErrorEmbed("You do not have enough boost.\n" +
                                                       $"Have: {contextPlayer.Data.Boost}\n" +
                                                       $"Need: {(int)(70 * (1 - contextPlayer.Data.DemoChance))}"));
                return;
            }

            string title, replyMsg, imgUrl = null;

            Color color;
            if (targetWasDemoed)
            {
                color = Bot.SuccessColor;
                title = "**BOOM!**";
                var demoMsgs = await _demoMsgs.ListDocumentsAsync().ToList();

                if (target.Id == 226543536302981120)
                {
                    replyMsg = ":boom: You demoed Sledge... but no one will believe you. :boom:";
                }

                else
                {
                    replyMsg = (await demoMsgs.ElementAt(new Random().Next(0, demoMsgs.Count)).GetSnapshotAsync()).GetValue<string>("content");

                    replyMsg = ":boom: " + replyMsg.Replace("%t", target.Username) + " :boom:";
                }

                if (Context.User.Id == 226543536302981120)
                {
                    imgUrl = "https://preview.redd.it/7s7kdercffe01.png?width=960&crop=smart&auto=webp&s=41c7e0940924a374fe0580d7fee87a90a7890763";
                }
            }

            else
            {
                var missMsgs = await _missMsgs.ListDocumentsAsync().ToList();

                replyMsg = (await missMsgs.ElementAt(new Random().Next(0, missMsgs.Count - 1)).GetSnapshotAsync()).GetValue<string>("content");
                replyMsg = ":dash: " + replyMsg.Replace("%t", target.Username) + " :dash:";

                color = Bot.ErrorColor;
                title = "*Woosh...*";

                if (target.Id == 226543536302981120)
                    imgUrl = "https://i.imgur.com/6u7arkT.png";
            }

            await base.ReplyAsync(embed: new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    IconUrl = Context.User.GetAvatarUrl(),
                    Name = Context.User.Username
                },
                Title = title,
                Color = color,
                Description = replyMsg,
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = "https://www.bing.com/th?id=OIP.rZ2p5dLAJ4ozk5UNfvs12QHaHa&pid=Api&rs=1&p=0",
                    Text = $"You used {boostUsed} boost.",
                },
                ImageUrl = imgUrl
            }.Build());

            await Bot.demoLeaderboard.UpdateAsync();
            await Bot.KdrLeaderboard.UpdateAsync();
        }

        [Command("stats")]
        [Summary("Checks your or another player's demo stats (demos and deaths)")]
        public async Task CheckDemoStatsAsync([Remainder] SocketGuildUser user = null)
        {
            if (user is null) user = Context.User as SocketGuildUser;

            if (user.IsBot)
            {
                await Context.Message.AddReactionAsync(Bot.ErrorEmote);
                return;
            }

            Player player = await Player.GetAsync(user.Id, _playerColl);

            string kdr = string.Format("{0:0.0#}", (double)player.Data.DemoCount / player.Data.DeathCount);

            await ReplyAsync(embed: new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    IconUrl = user.GetAvatarUrl(),
                    Name = user.Username
                },
                Color = Bot.SuccessColor,
                Fields =
                {
                    new EmbedFieldBuilder()
                    {
                        Name = "Demos",
                        Value = player.Data.DemoCount,
                        IsInline = true
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Deaths",
                        Value = player.Data.DeathCount,
                        IsInline = true
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "KDR",
                        Value = player.Data.DeathCount > 0 ? kdr : double.PositiveInfinity.ToString(),
                        IsInline = true
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Avoids",
                        Value = player.Data.AvoidCount,
                        IsInline = true
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Misses",
                        Value = player.Data.MissCount,
                        IsInline = true
                    }
                }
            }.Build());
        }

#if !DEBUG
        [RequireRole(493510105833144332)] //Require Mod Team role or above
#endif
        [Summary("Add a demo message to the database. Note: Use `%t` for target username.")]
        [Command("adddemomsg")]
        [Alias("adm")]
        public async Task AddDemoMessageAsync([Remainder] string msg)
        {
            await AddResponseMessageAsync(_demoMsgs, msg);
            await Context.Message.AddReactionAsync(Bot.SuccessEmote);
        }

#if !DEBUG
        [RequireRole(493510105833144332)] //Require Mod Team role or above
#endif
        [Summary("Add a miss message to the database. Note: use `%t` for target username.")]
        [Command("addmissmsg")]
        [Alias("amm")]
        public async Task AddMissMessageAsync([Remainder] string msg)
        {
            await AddResponseMessageAsync(_missMsgs, msg);
            await Context.Message.AddReactionAsync(Bot.SuccessEmote);
        }

        public async Task AddResponseMessageAsync(CollectionReference messages, string msg)
        {
            var x = await messages.ListDocumentsAsync().ToList();

            await messages.Document((x.Count + 1).ToString()).SetAsync(
                new Dictionary<string, object>()
                {
                    { "content", msg }
                });
        }
    }
}
