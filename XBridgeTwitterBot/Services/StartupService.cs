using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XBridgeTwitterBot.Config;

namespace XBridgeTwitterBot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IOptions<DiscordCredentials> _discordCredentials;

        public StartupService(DiscordSocketClient discord, IOptions<DiscordCredentials> discordCredentials)
        {
            _discord = discord;
            _discordCredentials = discordCredentials;
        }

        public async Task StartAsync()
        {
            string discordToken = _discordCredentials.Value.Token;
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token into the `_configuration.json` file found in the applications root directory.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);    
            await _discord.StartAsync();                                
        }
    }
}
