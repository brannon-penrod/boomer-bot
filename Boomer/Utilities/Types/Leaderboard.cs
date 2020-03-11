using Discord;
using Discord.Rest;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer
{
    public class Leaderboard
    {
        public LeaderboardType Type { get; private set; }
        public List<Player> TopPlayers { get; set; }
        public CollectionReference PlayerCollection { get; private set; }
        public int NumDisplayedPlayers { get; private set; }
        public RestUserMessage LeaderboardMessage { get; private set; }

        private Leaderboard() {}

        public static async Task<Leaderboard> CreateAsync(LeaderboardType type, int numDisplayedPlayers, 
                                                          ITextChannel leaderboardChannel, CollectionReference players)
        {
            Leaderboard l = new Leaderboard();
            await l.InitializeAsync(type, numDisplayedPlayers, leaderboardChannel, players);
            return l;
        }

        private async Task InitializeAsync(LeaderboardType type, int numPlayers, 
                                           ITextChannel leaderboardChannel, CollectionReference players)
        {
            PlayerCollection = players;
            NumDisplayedPlayers = numPlayers;
            Type = type;

            await UpdateDataAsync();
            await UpdateDisplayAsync(leaderboardChannel);
        }

        public async Task UpdateAsync()
        {
            await UpdateDataAsync();
            await UpdateDisplayAsync();
        }

        private async Task UpdateDataAsync()
        {
            List<Player> players = new List<Player>();
            var playerSnap = await PlayerCollection.GetSnapshotAsync();

            foreach (var player in playerSnap)
            {
                if (!ulong.TryParse(player.Id, out ulong id)) throw new Exception("Failed to parse player ID.");
                players.Add(await Player.GetAsync(id, PlayerCollection));
            }

            for (var i = 0; i < NumDisplayedPlayers; i++)
            {
                Player toRemove = null;

                TopPlayers.Add(players.Aggregate((l, r) =>
                {
                    switch (Type)
                    {
                        case LeaderboardType.Demo:
                            toRemove = l.Data.DemoCount > r.Data.DemoCount ? l : r;
                            break;
                        case LeaderboardType.KDR:
                            toRemove = l.Data.DemoCount / l.Data.DeathCount > r.Data.DemoCount / r.Data.DeathCount ? l : r;
                            break;
                        case LeaderboardType.BoostUsed:
                            toRemove = l.Data.BoostUsed > r.Data.BoostUsed ? l : r;
                            break;
                        default:
                            return null;
                    }

                    return toRemove;
                }));

                players.Remove(toRemove);
            }
        }

        private async Task UpdateDisplayAsync()
        {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

            for(int i = 0; i < TopPlayers.Count; i++)
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"{i}. {TopPlayers.ElementAt(i)}",
                    Value = $""
                });
            }

            await LeaderboardMessage.ModifyAsync(new Action<MessageProperties>(x =>
            {
                x.Embed = new EmbedBuilder()
                {
                    Title = Type.ToString(),
                    Fields = fields,
                    Color = Bot.SuccessColor,
                }.Build();
                x.Content = "";
            }));
        }

        private async Task UpdateDisplayAsync(ITextChannel leaderboardChannel)
        {
            LeaderboardMessage = (RestUserMessage)await leaderboardChannel.SendMessageAsync("Loading leaderboard. Please wait...");
            await UpdateDisplayAsync();
        }
    }
}
