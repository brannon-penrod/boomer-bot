using Discord;
using Discord.Commands;
using Google.Cloud.Firestore;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Boomer.Modules
{
    public class Info : ModuleBase<SocketCommandContext>
    {
        private readonly FirestoreDb _database;
        private readonly DocumentReference _sledgeInfo;

        public Info(FirestoreDb database)
        {
            _database = database;
            _sledgeInfo = _database.Document("misc-data/sledge-info");
        }
#if !DEBUG
        [RequireRole(394955577391972373)]
#endif
        [Command("setstats")]
        [Summary("Sets your (Sledge's) demo/exterminator count.")]
        public async Task SetDemosAsync(int demosToSet, int extermsToSet)
        {
            await _sledgeInfo.SetAsync(new Dictionary<string, object>()
            {
                { "demos", $"{demosToSet:###,###}" },
                { "exterminators", $"{extermsToSet:###,###}" },
                { "last-updated", DateTime.UtcNow.ToString("MMMM dd, yyyy") }
            });

            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("sledge")]
        [Summary("Gives some information about Rocket Sledge.")]
        public async Task GetSledgeInfoAsync()
        {
            HtmlWeb web = new HtmlWeb(); //new web client
            string url = "https://socialblade.com/youtube/channel/UC9t5ff1rz71eC4ZObmq6Yqg";

            var doc = await Task.Factory.StartNew(() => web.Load(url)); //load socialblade page

            HtmlNode subCountNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"youtube-stats-header-subs\"]"); //get subcount
            ulong.TryParse(subCountNode.InnerText, out ulong sc); //try to parse the sub count into a ulong
            string subCount = string.Format("{0:###,###}", sc); //format like "100,000"

            HtmlNode videoCountNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"youtube-stats-header-uploads\"]"); //get video count
            ulong.TryParse(videoCountNode.InnerText, out ulong vdc); //try to parse the video count into a ulong
            string videoCount = string.Format("{0:###,###}", vdc); //format like "100,000"

            HtmlNode viewCountNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"youtube-stats-header-views\"]"); //view count
            ulong.TryParse(viewCountNode.InnerText, out ulong vwc); //try to parse the view count into a ulong
            string viewCount = string.Format("{0:###,###}", vwc); //format like "100,000"

            string youtubeInfo = $"https://www.youtube.com/c/RocketSledge98 \n" +
                                 $"{subCount} subscribers\n" +
                                 $"{viewCount} total views\n" +
                                 $"{videoCount} videos";

            string gfycatUrl = "https://gfycat.com/@sledge98";

            var doc2 = await Task.Factory.StartNew(() => web.Load(gfycatUrl)); //load gfycat page

            HtmlNode gfycatViewCountNode =
                doc2.DocumentNode.SelectSingleNode("//*[@class=\"datum views\"]/span"); //get view count
            string gfycatViews = gfycatViewCountNode.InnerText;

            var info = (await _sledgeInfo.GetSnapshotAsync()).ToDictionary();

            if (!info.TryGetValue("demos", out object demoCount))
                demoCount = "0";
            if (!info.TryGetValue("exterminators", out object extermCount))
                extermCount = "0";
            if (!info.TryGetValue("last-updated", out object lastUpdated))
                lastUpdated = "never";

            string iconUrl = "https://yt3.ggpht.com/a-/AN66SAzJp28D-ydHwNB-o2giM1WtgO2mZhwn19T2AA=s88-mo-c-c0xffffffff-rj-k-no";

            await Context.Message.DeleteAsync();

            await Context.User.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "Rocket Sledge Stats & Information",
                Fields = {
                    new EmbedFieldBuilder(){
                        Name = "YouTube",
                        Value = youtubeInfo
                    },

                    new EmbedFieldBuilder() {
                        Name = "gfycat",
                        Value = $"{gfycatUrl}\n" +
                                $"{gfycatViews} total gif views"
                    },

                    new EmbedFieldBuilder(){
                        Name = "Rocket League",
                        Value = $"{demoCount.ToString()} Demos\n" +
                                $"{extermCount.ToString()} Exterminators"
                    }
                },

                Footer = new EmbedFooterBuilder().WithText($"Last updated {lastUpdated.ToString()}"),
                ThumbnailUrl = iconUrl,
                Color = Bot.SuccessColor
            }.Build());
        }

        [Command("support")]
        [Summary("Sends you some links with which you can support Sledge.")]
        public async Task SendSupportDMAsync()
        {
            await Context.Message.DeleteAsync();
            await Context.User.SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "Want to support Rocket Sledge?",
                Fields =
                {
                    new EmbedFieldBuilder()
                    {
                        Name = "Exclusive Discord Role, Inside Looks/Sneak Peaks, and Group Up with Sledge:",
                        Value = "https://www.patreon.com/rocket_sledge"
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Subscribe on YouTube:",
                        Value = "https://www.youtube.com/c/RocketSledge98"
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Follow on Twitter:",
                        Value = "https://www.twitter.com/Rocket_Sledge"
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = "Reddit: ",
                        Value = "https://www.reddit.com/user/sledge98/"
                    }
                },
                Color = Bot.SuccessColor,
                ThumbnailUrl = "https://yt3.ggpht.com/a-/AN66SAzJp28D-ydHwNB-o2giM1WtgO2mZhwn19T2AA=s88-mo-c-c0xffffffff-rj-k-no"
            }.Build());
        }
    }
}
