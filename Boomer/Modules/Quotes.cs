using Boomer.Utilities;
using Discord;
using Discord.Commands;
using Google.Api.Gax;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Boomer.Bot;

namespace Boomer.Modules
{
    public class Quotes : ModuleBase<SocketCommandContext>
    {
        private readonly FirestoreDb _database;
        private readonly CollectionReference _salt, _sugar;

        public Quotes(FirestoreDb database)
        {
            _database = database;
            _salt = _database.Collection("saltQuotes");
            _sugar = _database.Collection("sugarQuotes");
        }
#if !DEBUG
        [RequireChannel(new ulong[]{
            740388406407987282, // #🗻-salt-mountain
            740379586558296154 // #bot-test
        })]
#endif
        [Command("quote", ignoreExtraArgs: false)]
        [Summary("Displays a random salt *or* sugar quote.")]
        public async Task DisplayQuoteAsync()
        {
            await DisplayQuoteAsync(new Random().Next(0, 2) == 0 ? _salt : _sugar, 0);
        }
#if !DEBUG
        [RequireChannel(new ulong[] {
            740388406407987282, // #🗻-salt-mountain
            740379586558296154  // #bot-testing
        })]
#endif
        [Command("salt")]
        [Summary("Displays a random salt quote, or the quote with the specified id.")]
        public async Task DisplaySaltAsync(int id = 0)
        {
            await DisplayQuoteAsync(_salt, id);
        }
#if !DEBUG
        [RequireChannel(new ulong[]{
            740388406407987282, // #🗻-salt-mountain
            740379586558296154 // #bot-testing
        })]
#endif
        [Command("sugar")]
        [Summary("Displays a random sugar quote, or the quote with the specified id.")]
        public async Task DisplaySugarAsync(int id = 0)
        {
            await DisplayQuoteAsync(_sugar, id);
        }
#if !DEBUG
        [RequireRole(739322852813307925)] // Require Moderator role or above
#endif
        [Command("addquote")]
        [Summary("Adds a quote of the specified type (salt/sugar) from the specified context.")]
        public async Task AddQuoteAsync(string type, string context, [Remainder] string content)
        {
            if (!Enum.TryParse(context, true, out Quote.Context quoteContext))
                quoteContext = Quote.Context.Other;

            // determine which collection to use
            type = type.ToLower();

            CollectionReference quotes = 
                         type == "salt" ? _salt :
                         type == "sugar" ? _sugar :
                         null;

            int id = await FindLowestAvailableIdAsync(quotes);

            // add the quote/context/id
            Dictionary<string, object> newQuote = new Dictionary<string, object>()
            {
                { "content", content },
                { "context", quoteContext },
                { "id", id }
            };

            await quotes.Document().SetAsync(newQuote);

            Enum.TryParse(context, true, out Quote.Context embedContext);

            await ReplyAsync("Quote added!");
            await DisplayQuoteAsync(quotes, id);
        }
#if !DEBUG
        [RequireRole(739322852813307925)] // Require Moderator role or above
#endif
        [Command("delquote")]
        [Summary("Deletes a quote with the specified id from the salt or sugar quotes.")]
        public async Task DeleteQuoteAsync(string type, int id)
        {
            type = type.ToLower();
            CollectionReference quotes = type == "salt" ? _salt : type == "sugar" ? _sugar : null;

            var quote = await GetQuoteAsync(quotes, id);

            if (quotes is null)
            {
                await ReplyAsync(embed: ErrorEmbed("Invalid type."));
                return;
            }

            if (quote is null)
            {
                await ReplyAsync(embed: ErrorEmbed($"No quote found with quote {id}."));
                return;
            }

            await ReplyAsync("Deleting quote:");
            await DisplayQuoteAsync(quotes, id);

            await quote.Reference.DeleteAsync();

            await Context.Message.AddReactionAsync(SuccessEmote);
        }
#if !DEBUG
        [RequireRole(739322852813307925)] // Require Moderator role or above
#endif
        [Command("quotelist")]
        [Summary("Sends a link to view all of the quotes in the database.")]
        public async Task ListQuotesAsync()
        {
            await Context.User.SendMessageAsync("To view all of the quotes currently stored, go to the database here:" +
                "https://console.firebase.google.com/u/1/project/boomer-bc7c3/database/firestore/data~2FsaltQuotes~2F10109");
        }

        private async Task<DocumentSnapshot> GetQuoteAsync(CollectionReference quotes, int id)
        {
            var matches = quotes.WhereEqualTo("id", id);

            var docs = (await matches.GetSnapshotAsync()).Documents;

            if(docs.Count > 1)
            {
                throw new Exception($"More than one match for id {id} found.");
            }

            return docs.First();
        }

        private async Task DisplayQuoteAsync(CollectionReference quotes, int id)
        {
            string type = quotes == _salt ? "salt" : quotes == _sugar ? "sugar" : throw new ArgumentException();

            if (id == 0)
            {
                // generate a random id
                var list = await quotes.ListDocumentsAsync().ToListAsync()
                    ?? new List<DocumentReference>();

                if (list.Count() == 0)
                {
                    await ReplyAsync(embed: ErrorEmbed($"No {type} quotes found."));
                    return;
                }

                var randQuote = list[new Random().Next(0, list.Count())];

                var snap = await randQuote.GetSnapshotAsync();

                if(!snap.TryGetValue("id", out int randId))
                {
                    int newId = await FindLowestAvailableIdAsync(quotes);
                    await randQuote.UpdateAsync("id", newId);
                    randId = newId;
                }

                id = randId;
            }

            var toDisplay = await GetQuoteAsync(quotes, id);

            Dictionary<string, object> quote;
            if (toDisplay is null)
            {
                await ReplyAsync(embed: ErrorEmbed("No quote found with id " + id));
                return;
            }

            else quote = toDisplay.ToDictionary();

            if (quote.Count == 0)
            {
                await ReplyAsync(embed: ErrorEmbed($"No {type} quotes available. Try adding some using **addquote**."));
            }

            Enum.TryParse(quote["context"].ToString(), true, out Quote.Context context);

            string contextStr;
            switch (context)
            {
                case Quote.Context.RocketLeague:
                    contextStr = "Rocket League chat";
                    break;
                case Quote.Context.Community:
                    contextStr = "Community submissions";
                    break;
                default:
                    contextStr = context.ToString();
                    break;
            }

            await ReplyAsync(embed: new EmbedBuilder()
            {
                Color = type == "salt" ? SaltColor : SugarColor,
                Description = quote["content"].ToString(),
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = Quote.FooterIconUrl(context),
                    Text = $"From {contextStr} • ID: {id}"
                }
            }.Build());
        }

        private async Task<int> FindLowestAvailableIdAsync(CollectionReference quotes)
        {
            int id = 1;

            // generate the lowest-integer ID not already contained in the collection.

            List<DocumentSnapshot> quoteList = (await quotes.OrderBy("id").GetSnapshotAsync()).ToList();

            foreach (DocumentSnapshot q in quoteList)
            {
                if (q.TryGetValue("id", out int qId))
                {
                    if (qId <= id) id = qId + 1;
                    else return id;
                }
            }

            return id;
        }
    }
}
