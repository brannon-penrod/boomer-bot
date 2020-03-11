using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer
{
    public class Player
    {
        public DemoData Data { get; private set; }
        public List<DbMessage> RecentMessages { get; set; }

        private CollectionReference PlayerCollection;
        private DocumentReference PlayerDoc { get; set; }
        private Player() { }

        public static async Task<Player> GetAsync(ulong id, CollectionReference playerCollection)
        {
            Player p = new Player();
            await p.InitializeAsync(id, playerCollection);
            return p;
        }

        private async Task InitializeAsync(ulong id, CollectionReference playerCollection)
        {
            PlayerCollection = playerCollection;
            PlayerDoc = PlayerCollection.Document($"players/{id}");

            var playerDocSnap = await PlayerDoc.GetSnapshotAsync();

            if (!playerDocSnap.Exists)
            {
                await PlayerDoc.CreateAsync(new Dictionary<string, object>()
                {
                    { "boost", 0 },
                    { "demo-count", 0 },
                    { "death-count", 0 },
                    { "demo-rate", (DemoData.MinDemoChance + DemoData.MaxDemoChance) / 2 },
                    { "avoid-rate", (DemoData.MinAvoidChance + DemoData.MaxAvoidChance) / 2 },
                    { "avoid-count", 0 },
                    { "miss-count", 0 }
                });

                playerDocSnap = await PlayerDoc.GetSnapshotAsync();
            }

            if(!playerDocSnap.ContainsField("avoid-count"))
            {
                await PlayerDoc.UpdateAsync("avoid-count", 0);
                await PlayerDoc.UpdateAsync("miss-count", 0);
            }

            if (!playerDocSnap.TryGetValue("avoid-count", out int avoidCount))
                avoidCount = 0;

            if (!playerDocSnap.TryGetValue("miss-count", out int missCount))
                missCount = 0;

            if (!playerDocSnap.TryGetValue("boost-used", out int boostUsed))
                boostUsed = 0;

            Data = new DemoData(
                    playerDocSnap.GetValue<int>("boost"),
                    playerDocSnap.GetValue<int>("demo-count"),
                    playerDocSnap.GetValue<int>("death-count"),
                    playerDocSnap.GetValue<double>("demo-rate"),
                    playerDocSnap.GetValue<double>("avoid-rate"),
                    avoidCount,
                    missCount);

            RecentMessages = new List<DbMessage>();

            var recentMsgSnap = await PlayerDoc.Collection("recent-messages").GetSnapshotAsync();

            if (recentMsgSnap.Count > 0)
            {
                foreach (var message in recentMsgSnap)
                {
                    //content, createTime
                    if (!message.TryGetValue("content", out string content))
                        content = "";
                    if (!message.TryGetValue("create-time", out DateTime createTime))
                        createTime = new DateTime(0);
                    if (!message.TryGetValue("is-successful-command", out bool isSuccessfulCommand))
                        isSuccessfulCommand = false;

                    RecentMessages.Add(await DbMessage.CreateAsync(content, createTime, isSuccessfulCommand, id, PlayerCollection, message.Id));
                }
            }
        }

        public async Task AddBoostAsync(SocketCommandContext context)
        {
            double rand = new Random().NextDouble();
            int toAdd;

            double probFor12 = 0.05, probFor20 = probFor12 + 0.001;
                
            if (rand <= probFor12)
            {
                toAdd = 12;
                await context.Message.AddReactionAsync(new Emoji("💥"));
            }

            else if (rand <= probFor20)
            {
                toAdd = 20;
                await context.Channel.SendMessageAsync($"{context.User.Mention} found a 20 boost pad!");
            }

            else toAdd = 1;

            Data.Boost = Util.Clamp(Data.Boost + toAdd, 0, 100);

            await PlayerDoc.UpdateAsync("boost", Data.Boost);
        }

        public async Task LogMessageAsync(SocketUserMessage msg, bool isSuccessfulCommand)
        {
            RecentMessages.Add(await DbMessage.CreateAsync(msg, PlayerCollection, isSuccessfulCommand));

            while (RecentMessages.Count > 5)
            {
                var oldest = RecentMessages.Aggregate((l, r) => l.CreateTime < r.CreateTime ? l : r);

                await oldest.DeleteAsync();
                RecentMessages.Remove(oldest);
            }
        }

        public async Task<(bool enoughBoost, bool targetWasDemoed, int boostUsed)> AttemptDemoAsync(Player target)
        {
            Random rand = new Random();

            bool enoughBoost = Data.Boost >= Math.Floor(70 * (1 - Data.DemoChance));
            if (!enoughBoost) return (false, false, 0);

            int boostUsed = (int)(rand.Next(60, 70) * (1 - Data.DemoChance));

            Data.Boost = (Data.Boost - boostUsed).Clamp(0, 100);

            await PlayerDoc.UpdateAsync("boost", Data.Boost);

            var probOfSuccess = Data.DemoChance * (1 - target.Data.AvoidChance) + rand.Next(0, 200) / 1000;
            var targetWasDemoed = rand.NextDouble() - probOfSuccess > 0;
            double senderAvoidDecrease, senderDemoIncrease, targetAvoidIncrease, targetDemoDecrease;

            if (targetWasDemoed)
            {
                await PlayerDoc.UpdateAsync("demo-count", ++Data.DemoCount);
                await target.PlayerDoc.UpdateAsync("death-count", ++target.Data.DeathCount);

                senderDemoIncrease = rand.Next(0, 300) / Math.Pow(10, 5);
                senderAvoidDecrease = -(rand.Next(0, 100) / Math.Pow(10, 5));

                targetDemoDecrease = -(rand.Next(0, 100) / Math.Pow(10, 5));
                targetAvoidIncrease = rand.Next(0, 100) / Math.Pow(10, 5);
            }

            else
            {
                await PlayerDoc.UpdateAsync("miss-count", ++Data.MissCount);
                await target.PlayerDoc.UpdateAsync("avoid-count", ++target.Data.AvoidCount);
                senderDemoIncrease = rand.Next(0, 100) / Math.Pow(10, 5);
                senderAvoidDecrease = -(rand.Next(0, 100) / Math.Pow(10, 5));

                targetDemoDecrease = -(rand.Next(0, 100) / Math.Pow(10, 5));
                targetAvoidIncrease = rand.Next(0, 300) / Math.Pow(10, 5);
            }

            await PlayerDoc.UpdateAsync("demo-rate", (Data.DemoChance += senderDemoIncrease).Clamp(DemoData.MinDemoChance, DemoData.MaxDemoChance));
            await PlayerDoc.UpdateAsync("avoid-rate", (Data.AvoidChance -= senderAvoidDecrease).Clamp(DemoData.MinAvoidChance, DemoData.MaxAvoidChance));

            await
                target.PlayerDoc.UpdateAsync("demo-rate", (target.Data.DemoChance -= targetDemoDecrease).Clamp(DemoData.MinDemoChance, DemoData.MaxDemoChance));
            await
                target.PlayerDoc.UpdateAsync("avoid-rate", (target.Data.AvoidChance += targetAvoidIncrease).Clamp(DemoData.MinAvoidChance, DemoData.MaxAvoidChance));

            return (enoughBoost, targetWasDemoed, boostUsed);
        }
    }
}
