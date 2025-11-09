using Discord.Interactions;

namespace DiscordBotService.Discord.Commands;

/// <summary>
/// Example commands
/// </summary>
/// <param name="logger">Logger in the DebugCommands category</param>
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
