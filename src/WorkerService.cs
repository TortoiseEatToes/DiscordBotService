using DiscordBotService.Discord;
using DiscordBotService.Secrets;

namespace DiscordBotService;

/// <summary>
/// Service for running the Discord Bot
/// </summary>
/// <param name="logger">Logger in the WorkerService category</param>
/// <param name="discordBot">Reference to our custom discord bot</param>
/// <param name="serviceProvider">The service provider that created this class</param>
public class WorkerService(
    ILogger<WorkerService> logger,
    IDiscordBot discordBot,
    IServiceProvider serviceProvider) : BackgroundService
{
    /// <summary>
    /// DiscordToken key
    /// </summary>
    public const string TokenStringName = "DiscordToken";

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting up");
        logger.LogDebug("Getting discord token '{0}'", TokenStringName);
        var secretsManager = serviceProvider.GetRequiredService<ISecretsManager>();
        string discordToken = secretsManager.GetRequiredSecret(TokenStringName);
        try
        {
            await discordBot.StartAsync(discordToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch(Exception exception)
        {
            logger.LogCritical(exception, "WorkerService shutting down with exception");
        }
        finally
        {
            logger.LogInformation("Shutting down");
            await discordBot.StopAsync();
        }
    }
}
