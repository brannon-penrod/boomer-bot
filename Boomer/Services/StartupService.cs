using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Boomer
{
    class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _services;

        public StartupService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider services)
        {
            _config = config;
            _discord = discord;
            _commands = commands;
            _services = services;
        }

        public async Task StartAsync()
        {
#if !DEBUG
            var guilds = _discord.Guilds;

            if (guilds.Any(g => g.Id != 738929882846855168))
            {
                foreach (var guild in guilds.Where(g => g.Id != 738929882846855168))
                {
                    await guild.LeaveAsync();
                } 
            }
#endif

            string discordToken = _config["tokens:discord"];

            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token info into the `_configuration.json` file found in the application's root directory.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _discord.SetGameAsync($"{_config["prefix"]}help", null, ActivityType.Listening);
        }
    }
}
