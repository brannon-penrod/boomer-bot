using Discord;
using Discord.Commands;
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
            //#salt,
            //#hypos-bot-control
        })]
#endif
        [Command("quote", ignoreExtraArgs: false)]
        [Summary("Displays a random salt *or* sugar quote.")]
        public async Task DisplayQuoteAsync()
        {
            await DisplayQuoteAsync(new Random().Next(0, 2) == 0 ? _salt : _sugar, 0, Context);
        }
#if !DEBUG
        [RequireChannel(new ulong[]{
            //#salt ID,
            //#hypos-bot-control
        })]
#endif
        [Command("salt")]
        [Summary("Displays a random salt quote, or the quote with the specified id.")]
        public async Task DisplaySaltAsync(int id = 0)
        {
            await DisplayQuoteAsync(_salt, id, Context);
        }
#if !DEBUG
        [RequireChannel(new ulong[]{
            //#salt ID,
            //#hypos-bot-control
        })]
#endif
        [Command("sugar")]
        [Summary("Displays a random sugar quote, or the quote with the specified id.")]
        public async Task DisplaySugarAsync(int id = 0)
        {
            await DisplayQuoteAsync(_sugar, id, Context);
        }
#if !DEBUG
        [RequireRole(493510105833144332)] //Require Mod role or above
#endif
        [Command("addquote")]
        [Summary("Adds a quote of the specified type (salt/sugar) from the specified context.")]
        public async Task AddQuoteAsync(string type, string context, [Remainder] string content)
        {
            if (!Enum.TryParse(context, true, out Quote.Context quoteContext))
                quoteContext = Quote.Context.Other;

            //determine which collection to use
            type = type.ToLower();
            var quotes = type == "salt" ? _salt :
                         type == "sugar" ? _sugar :
                         null;

            if (quotes is null)
            {
                await ReplyAsync("Type must be one of salt or sugar.");
                return;
            }

            QuerySnapshot snapshot = await quotes.GetSnapshotAsync();

            //generate a random 5-digit ID not already contained in the collection
            Random rand = new Random();
            int id = 0;

            do
            {
                id = rand.Next(1, 99999);
            } while (snapshot.Any(x => x.Id == id.ToString()));

            //add the quote and context
            Dictionary<string, object> newQuote = new Dictionary<string, object>()
            {
                { "content", content },
                { "context", quoteContext }
            };

            await quotes.Document(id.ToString()).SetAsync(newQuote);

            Enum.TryParse(context, true, out Quote.Context embedContext);

            await ReplyAsync("Quote added!");
            await DisplayQuoteAsync(quotes, id, Context);
        }
#if !DEBUG
        [RequireRole(493510105833144332)]
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
            await DisplayQuoteAsync(quotes, id, Context);

            await quote.DeleteAsync();

            await Context.Message.AddReactionAsync(SuccessEmote);
        }
#if !DEBUG
        [RequireRole(493510105833144332)]
#endif
        [Command("quotelist")]
        [Summary("Sends a link to view all of the quotes in the database.")]
        public async Task ListQuotesAsync()
        {
            await Context.User.SendMessageAsync("To view all of the quotes currently stored, go to the database here:" +
                "https://console.firebase.google.com/u/1/project/boomer-bc7c3/database/firestore/data~2FsaltQuotes~2F10109");
        }

        private async Task<DocumentReference> GetQuoteAsync(CollectionReference quotes, int id)
        {
            var quoteList = await quotes.ListDocumentsAsync().ToList()
                ?? new List<DocumentReference>();
            var matches = quoteList.Where(x => x.Id == id.ToString());

            if (matches.Count() == 0) return null;
            else return matches.First();
        }

        private async Task DisplayQuoteAsync(CollectionReference quotes, int id, SocketCommandContext scc)
        {
            string type = quotes == _salt ? "salt" : quotes == _sugar ? "sugar" : throw new ArgumentException();

            if (id == 0)
            {
                //generate a random id
                var list = await quotes.ListDocumentsAsync().ToList()
                    ?? new List<DocumentReference>();

                if (list.Count() == 0)
                {
                    await ReplyAsync(embed: ErrorEmbed($"No {type} quotes found."));
                    return;
                }

                if (!int.TryParse(list[new Random().Next(0, list.Count())].Id, out int randId))
                    throw new Exception("Id parse failed. Please check the Firestore.");

                id = randId;
            }

            // get a dictionary representation of the quote with the id
            _ = new Dictionary<string, object>();

            var x = await GetQuoteAsync(quotes, id);
            Dictionary<string, object> quote;
            if (x is null)
            {
                await ReplyAsync(embed: ErrorEmbed("No quote found with id " + id));
                return;
            }

            else quote = (await x.GetSnapshotAsync()).ToDictionary();

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
                Description = $"*{quote["content"].ToString()}*",
                Footer = new EmbedFooterBuilder()
                {
                    IconUrl = Quote.FooterIconUrl(context),
                    Text = $"From {contextStr} • ID: {id}"
                }
            }.Build());
        }
    }
}
