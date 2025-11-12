using Discord.Interactions;
using DiscordBotCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace DiscordBotCommands.Commands;

/// <summary>
/// Example commands
/// </summary>
/// <param name="logger">Logger in the DebugCommands category</param>
[GlobalModule]
public class DebugCommands(ILogger<DebugCommands> logger) : InteractionModuleBase
{
    /// <summary>
    /// Responds with "pong"
    /// </summary>
    [SlashCommand("test_ping", "Verify the bot is responsive")]
    public async Task Ping()
    {
        logger.LogTrace("Ping called");
        await RespondAsync("pong");
    }
}
