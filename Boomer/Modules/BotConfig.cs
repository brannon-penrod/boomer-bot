using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Boomer.Modules
{
    public class BotConfig : ModuleBase<SocketCommandContext>
    {
        private readonly IConfigurationRoot _config;

        public BotConfig(IConfigurationRoot config)
        {
            _config = config;
        }
#if !DEBUG
        [RequireRole(739322852813307925)] // Require Moderator role or above
#endif
        [Command("setprefix")]
        [Summary("Sets this bot's prefix.")]
        public async Task SetPrefixAsync(string newPrefix)
        {
            _config["prefix"] = newPrefix;

            await Context.Message.AddReactionAsync(Bot.SuccessEmote);
            await Context.Client.SetGameAsync(newPrefix + "help");
        }
#if !DEBUG
        [RequireRole(739322852813307925)] // Require Moderator role or above
#endif
        [Command("setactivity")]
        [Summary("Set this bot's activity.\n" +
            "Valid activity types are listening, playing, streaming, and watching.\n" +
            "If the activityType is streaming, the last word after a space will be used as the stream URL.\n")]
        public async Task SetActivityAsync(string activityType, [Remainder] string title)
        {
            activityType = activityType.ToLower();

            ActivityType type;
            switch (activityType)
            {
                case "listening":
                    type = ActivityType.Listening;
                    break;
                case "playing":
                    type = ActivityType.Playing;
                    break;
                case "watching":
                    type = ActivityType.Watching;
                    break;
                case "streaming":
                    type = ActivityType.Streaming;
                    break;
                default:
                    await ReplyAsync("Valid activity types are listening, playing, streaming, and watching.");
                    return;
            }

            string streamUrl = null;

            if (activityType == "streaming")
            {
                string[] strs = title.Split(' ');
                if ((strs[strs.Length - 1]).Contains("http"))
                {
                    streamUrl = strs[strs.Length - 1];

                    strs[strs.Length - 1] = null;
                    title = null;
                    foreach (string s in strs)
                    {
                        title += s + " ";
                    }
                }
            }

            else if (activityType == "listening")
            {

            }

            await Context.Client.SetGameAsync(title, streamUrl, type);
            await Context.Message.AddReactionAsync(Bot.SuccessEmote);
        }
    }
}
