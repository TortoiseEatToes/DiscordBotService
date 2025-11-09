namespace DiscordBotService.Discord;

/// <summary>
/// Generic interface for the Discord bot.
/// </summary>
public interface IDiscordBot
{
    /// <summary>
    /// Starts the discord bot
    /// </summary>
    /// <param name="discordToken">Token for your Discord bot</param>
    Task StartAsync(string discordToken);

    /// <summary>
    /// Stops the discord bot
    /// </summary>
    Task StopAsync();
}
