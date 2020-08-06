using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Api;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Boomer.Modules
{
    public class Fun : ModuleBase<SocketCommandContext>
    {
        static readonly string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static readonly string dancePath = $@"{directory}\Media\DanceAnimation";

        public Fun()
        {

        }

        [Command("dance")]
        [Summary("Make the bot dance.")]
        public async Task DanceAsync()
        {
            var danceFiles = Directory.EnumerateFiles(dancePath, "*.txt").ToList();

            danceFiles.Sort();

            List<string> danceAnimation = new List<string>();

            foreach(var file in danceFiles)
            {
                danceAnimation.Add(File.ReadAllText(file));
            }

            var msg = await ReplyAsync(danceAnimation.First());

            foreach(var frame in danceAnimation)
            {
                await msg.ModifyAsync(x => x.Content = frame);

                await Task.Delay(1454);
            }

            await Task.Delay(1000);
            await msg.AddReactionAsync(new Emoji("💥"));
        }
    }
}
