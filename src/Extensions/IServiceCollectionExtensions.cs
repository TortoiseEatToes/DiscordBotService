using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBotService.Discord;
using DiscordBotService.Extensions;
using DiscordBotService.Secrets;
using NLog.Extensions.Logging;

namespace DiscordBotService.Extensions;

/// <summary>
/// Utility functions for required services
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds NLog as our standardized ILogger.
    /// </summary>
    /// <remarks>
    /// Clears existing logging providers
    /// </remarks>
    public static void AddNLog(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog();
        });
    }

    /// <summary>
    /// Adds the default <see cref="SecretsManager"/>
    /// </summary>
    /// <remarks>
    /// This adds environment variables and user secrets as secret sources.
    /// </remarks>
    public static void AddSecretsManager(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ISecretsManager, SecretsManager>();
    }

    /// <summary>
    /// Adds the Discord bot to the service provider.
    /// </summary>
    /// <remarks>
    /// Additionally adds a bunch of Discord bot dependencies.
    /// </remarks>
    public static void AddDiscordBot(this IServiceCollection serviceCollection)
    {
        var discordSocketConfig = new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogLevel = LogSeverity.Warning
        };
        serviceCollection.AddSingleton(discordSocketConfig);
        var discordSocketClient = new DiscordSocketClient(discordSocketConfig);
        serviceCollection.AddSingleton<IRestClientProvider>(discordSocketClient); //This is needed by the InteractionService
        serviceCollection.AddSingleton(discordSocketClient);

        var interactionServiceConfig = new InteractionServiceConfig {
            UseCompiledLambda = true,
            LogLevel = LogSeverity.Warning
        };
        serviceCollection.AddSingleton(interactionServiceConfig);
        serviceCollection.AddSingleton<InteractionService>();
        
        serviceCollection.AddSingleton<IDiscordBot, DiscordBot>();
    }
}
