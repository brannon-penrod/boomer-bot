namespace Boomer
{
    public class Quote
    {
        public enum Context
        {
            Discord,
            Reddit,
            RocketLeague,
            YouTube,
            Twitter,
            Twitch,
            Community,
            Other
        };

        public static string FooterIconUrl(Context context)
        {
            switch (context)
            {
                case Context.Discord:
                    return "https://www.logolynx.com/images/logolynx/1b/1bcc0f0aefe71b2c8ce66ffe8645d365.png";
                case Context.Reddit:
                    return "https://cdn.freebiesupply.com/logos/large/2x/reddit-2-logo-png-transparent.png";
                case Context.RocketLeague:
                    return "https://pbs.twimg.com/profile_images/999694973153902593/amj6rcPA_400x400.jpg";
                case Context.YouTube:
                    return "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e1/YouTube_play_buttom_icon_%282013-2017%29.svg/1280px-YouTube_play_buttom_icon_%282013-2017%29.svg.png";
                case Context.Twitter:
                    return "https://cdn1.iconfinder.com/data/icons/logotypes/32/square-twitter-512.png";
                case Context.Twitch:
                    return "https://vignette.wikia.nocookie.net/logopedia/images/8/83/Twitch_icon.svg/revision/latest?cb=20140727180700";
                case Context.Community:
                    return "https://pbs.twimg.com/profile_images/1058614313382891520/LU3jHaBg_400x400.jpg";
                default:
                    return null;
            }
        }
    }
}
