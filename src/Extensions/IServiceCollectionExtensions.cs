using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using NLog.Extensions.Logging;
using SharedUtilities.Secrets;
using TortoiseBot.Discord;

namespace TortoiseBot.Extensions;

/// <summary>
/// Utility functions for required services
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds NLog as our standardized ILogger
    /// </summary>
    public static void AddNLog(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog();
        });
    }

    /// <summary>
    /// Adds the default SecretsManager
    /// </summary>
    public static void AddSecretsManager(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ISecretsManager, SecretsManager>();
    }

    /// <summary>
    /// Adds the discord bot
    /// </summary>
    public static void AddDiscordBot(this IServiceCollection serviceCollection)
    {
        var discordSocketConfig = new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.AllUnprivileged,
            LogLevel = LogSeverity.Warning
        };
        serviceCollection.AddSingleton(discordSocketConfig);
        var discordSocketClient = new DiscordSocketClient(discordSocketConfig);
        serviceCollection.AddSingleton<IRestClientProvider>(discordSocketClient);
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
