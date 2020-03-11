using Discord;

namespace Boomer
{
    public class Bot
    {
        public static readonly IEmote SuccessEmote = new Emoji("✅");
        public static readonly IEmote ErrorEmote = new Emoji("❌");
        public static readonly IEmote TimeEmote = new Emoji("🕰");
        public static readonly Color SuccessColor = Color.Gold;
        public static readonly Color ErrorColor = Color.Red;

        public static readonly Color SaltColor = Color.Orange;
        public static readonly Color SugarColor = Color.Green;

        public static Leaderboard demoLeaderboard;
        public static Leaderboard KdrLeaderboard;

        public static Embed ErrorEmbed(string description = null)
        {
            return new EmbedBuilder()
            {
                Color = ErrorColor,
                Description = description
            }.Build();
        }

        public static Embed SuccessEmbed(string description = null, string footerImgUrl = null, string footerVal = null)
        {
            return new EmbedBuilder()
            {
                Color = SuccessColor,
                Description = description,
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = footerImgUrl,
                    Text = footerVal
                }
            }.Build();
        }
    }
}
