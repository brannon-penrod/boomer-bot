using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Boomer.Modules
{
    public class Help : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;

        public Help(CommandService service, IConfigurationRoot config)
        {
            _service = service;
            _config = config;
        }

        [Command("help", ignoreExtraArgs: false)]
        public async Task HelpAsync()
        {
            //command list builder
            var builder = new EmbedBuilder()
            {
                Color = Bot.SuccessColor,
            };
            builder.Description +=
                    $"Prefix: {_config["prefix"]}\n" +
                    $"[Required parameter]\n" +
                    $"<Optional parameter>\n";

            //add all commands' information to the help message
            foreach (ModuleInfo module in _service.Modules)
            {
                //to store module and command info
                string description = null;

                //add the command's information to the help message
                foreach (CommandInfo cmd in module.Commands)
                {
                    //the command has a summary
                    if (cmd.Summary != null)
                    {
                        //check if the user has permission to use the command
                        var result = await cmd.CheckPreconditionsAsync(Context);

                        //the user has permission
                        if (result.IsSuccess)
                        {
                            //add cmd's first alias to the description
                            description += $"**{cmd.Aliases.First()}**";

                            //add the parameter names and optionality
                            foreach (var param in cmd.Parameters)
                            {
                                if (param.IsOptional)
                                    description += $" *<{param.Name}>*";
                                else //the parameter is required
                                    description += $" *[{param.Name}]*";
                            }

                            //add command summary
                            description += $" :\n {cmd.Summary}\n";
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(description)) //something was added
                {
                    //add the module and command information to the help message builder
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await Context.Message.DeleteAsync();
            await Context.User.SendMessageAsync("", false, builder.Build());
        }

        [Command("help", ignoreExtraArgs: false)]
        public async Task HelpAsync([Remainder] string commands)
        {
            //command list builder
            var builder = new EmbedBuilder()
            {
                Color = Bot.SuccessColor,
            };

            //a command search was entered
            if (commands != null)
            {
                //separate the search terms by spaces
                string[] searches = commands.Split(' ');

                //conduct a search of the term, then add info if any is found
                foreach (string search in searches)
                {
                    //to store module and command info
                    string description = null;

                    //search for the search terms in Context
                    var result = _service.Search(Context, search);

                    if (result.IsSuccess)
                    {
                        //add the command's information to the help message
                        foreach (CommandMatch match in result.Commands)
                        {
                            //store the match's command
                            var cmd = match.Command;

                            //add cmd's first alias to the description
                            description += $"**{cmd.Aliases.First()}**";

                            //add the parameter names and optionality
                            foreach (ParameterInfo param in cmd.Parameters)
                            {
                                if (param.IsOptional)
                                    description += $" *<{param.Name}>";
                                else
                                    description += $" *[{param.Name}]";
                            }

                            //add the command summary 
                            description += $" :*\n {cmd.Summary}\n";
                            //note: be sure to add summaries to all commands you want to be displayed
                        }

                        //matches were found
                        if (!string.IsNullOrEmpty(description))
                        {
                            //add description to the builder's description
                            builder.Description += description;
                        }
                    }

                    else builder.Description += $"No command found with name {search}.\n";
                }

                await Context.Message.DeleteAsync();
                await Context.User.SendMessageAsync(embed: builder.Build());
            }
        }
    }
}
