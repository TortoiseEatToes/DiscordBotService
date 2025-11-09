using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;

namespace TortoiseBot.Discord;

/// <summary>
/// The discord bot for interacting with the Drafty WebAPI
/// </summary>
public class DiscordBot(ILogger<DiscordBot> logger, DiscordSocketClient discordSocketClient, InteractionService interactionService, IServiceProvider serviceProvider) : IDiscordBot, IAsyncDisposable
{
    /// <summary>
    /// Dispose flag
    /// </summary>
    private bool _isRunning = false;

    ///<inheritdoc/>
    public async Task StartAsync(string discordToken)
    {
        if (_isRunning)
        {
            logger.LogWarning("Discord bot was already started");
            return;
        }

        logger.LogDebug("DraftyDiscordBot Setting callbacks");
        discordSocketClient.Ready += OnClientIsReady;
        discordSocketClient.Log += OnClientLog;
        discordSocketClient.JoinedGuild += OnJoinedGuild;
        discordSocketClient.InteractionCreated += OnInteractionCreated;

        AddInteractionServiceCallbacks();

        logger.LogDebug("DraftyDiscordBot logging in");
        await discordSocketClient.LoginAsync(TokenType.Bot, discordToken);
        await discordSocketClient.StartAsync();
    }

    ///<inheritdoc/>
    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            logger.LogWarning("Discord bot was not started or already disposed");
            return;
        }
        logger.LogDebug("Shutting down Discord socket client");
        foreach (SocketGuild guild in discordSocketClient.Guilds)
        {
            logger.LogDebug($"DeleteApplicationCommandsAsync - {guild}");
            await guild.DeleteApplicationCommandsAsync();
            await Task.Delay(1000);
        }
        await discordSocketClient.LogoutAsync();
        await discordSocketClient.StopAsync();
    }

    ///<inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopAsync();
    }

    /// <exclude/>
    /// <summary>
    /// When we receive the OnClientIsReady event from Discord, create the modules on all of our servers
    /// </summary>
    private async Task OnClientIsReady()
    {
        ModuleInfo[] allModules = (await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider)).ToArray();

        logger.LogDebug("Setting up guild commands");
        foreach (var guild in discordSocketClient.Guilds)
        {
            logger.LogDebug($"registering modules to {guild.Name}");
            await interactionService.AddModulesToGuildAsync(guild.Id, deleteMissing: true, allModules);
            await Task.Delay(1000);
        }

        logger.LogDebug("DraftyDiscordBot is ready");
    }

    /// <exclude/>
    /// <summary>
    /// When this bot joins a guild, we add our commands to that guild.
    /// </summary>
    private Task OnJoinedGuild(SocketGuild socketGuild)
    {
        logger.LogDebug($"OnJoinedGuild: {socketGuild.Id}");
        interactionService.AddCommandsToGuildAsync(socketGuild.Id);
        return Task.CompletedTask;
    }

    /// <exclude/>
    /// <summary>
    /// Callback for when an interaction is created
    /// </summary>
    private async Task OnInteractionCreated(SocketInteraction socketInteraction)
    {
        try
        {
            var context = new SocketInteractionContext(discordSocketClient, socketInteraction);
            await interactionService.ExecuteCommandAsync(context, serviceProvider);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ExecuteCommandAsync failed");
            if (socketInteraction.Type == InteractionType.ApplicationCommand)
            {
                await socketInteraction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }

    /// <exclude/>
    /// <summary>
    /// Binds a <see cref="HandleInteractionExecuted{TInteraction}(TInteraction, IInteractionContext, IResult)"/> to all the Discord callbacks
    /// </summary>
    private void AddInteractionServiceCallbacks()
    {
        interactionService.SlashCommandExecuted += async (slashCommandInfo, interactionContext, result) => { await HandleInteractionExecuted(slashCommandInfo, interactionContext, result); };
        interactionService.ContextCommandExecuted += async (contextCommandInfo, interactionContext, result) => { await HandleInteractionExecuted(contextCommandInfo, interactionContext, result); };
        interactionService.ComponentCommandExecuted += async (componentCommandInfo, interactionContext, result) => { await HandleInteractionExecuted(componentCommandInfo, interactionContext, result); };
        interactionService.AutocompleteCommandExecuted += async (autoCompleteCommandInfo, interactionContext, result) => { await HandleInteractionExecuted(autoCompleteCommandInfo, interactionContext, result); };
        interactionService.AutocompleteHandlerExecuted += async (autoCompleteHandler, interactionContext, result) => { await HandleInteractionExecuted(autoCompleteHandler, interactionContext, result); };
        interactionService.ModalCommandExecuted += async (modalCommandInfo, interactionContext, result) => { await HandleInteractionExecuted(modalCommandInfo, interactionContext, result); };
    }

    /// <exclude/>
    /// <summary>
    /// Generic callback for logging information about issues with Discord interactions
    /// </summary>
    private async Task HandleInteractionExecuted<TInteraction>(TInteraction interaction, IInteractionContext interactionContext, IResult result)
    {
        if (result.IsSuccess)
        {
            logger.LogDebug("Interaction success: {0}:{1}", GetInteractionContextString(interactionContext), interaction);
            return;
        }
        string interactionName = interaction is null ? $"Unknown {nameof(InteractionType)}" : $"{interaction}";
        await HandleInteractionError(interactionName, interactionContext, result);
    }

    /// <exclude/>
    /// <summary>
    /// When we receive failed interactions, log out the information and try to respond if possible.
    /// </summary>
    private async Task HandleInteractionError(string interactionName, IInteractionContext interactionContext, IResult result)
    {
        string resultErrorMessage = GetResultErrorString(result);
        string interactionErrorMessage = $"{interactionName}:{resultErrorMessage}";
        logger.LogError($"{GetInteractionContextString(interactionContext)}:{interactionErrorMessage}");
        if (interactionContext.Interaction.HasResponded)
        {
            logger.LogError($"This interaction was already responded to.  Skipping sending a response.");
            return;
        }

        TimeSpan timeSinceCreated = DateTimeOffset.Now - interactionContext.Interaction.CreatedAt;
        if (timeSinceCreated.Seconds >= 2)
        {
            logger.LogError("This interaction is {0} seconds old.  Skipping response to safely avoid the 3 second limit from Discord.", timeSinceCreated.Seconds);
            return;
        }

        const string userErrorMessage = "Something went wrong.  Please contact a dev.";
        logger.LogDebug($"HandleInteractionError sending follow up to user: {userErrorMessage}");
        await interactionContext.Interaction.RespondAsync(userErrorMessage);
    }

    /// <exclude/>
    /// <summary>
    /// Converts an interaction to a convenient string
    /// </summary>
    private static string GetInteractionContextString(IInteractionContext interactionContext)
    {
        return $"{interactionContext.Guild}:{interactionContext.User}";
    }

    /// <exclude/>
    /// <summary>
    /// Converts a result to a convenient string
    /// </summary>
    private static string GetResultErrorString(IResult result)
    {
        return result.Error switch
        {
            InteractionCommandError.UnmetPrecondition => $"Unmet Precondition: {result.ErrorReason}",
            InteractionCommandError.UnknownCommand => "Unknown command",
            InteractionCommandError.BadArgs => "Invalid number or arguments",
            InteractionCommandError.Exception => $"Command exception: {result.ErrorReason}",
            InteractionCommandError.Unsuccessful => "Command could not be executed",
            _ => "Unknown error"
        };
    }

    /// <exclude/>
    /// <summary>
    /// All messages from Discord are sent through our logger as well
    /// </summary>
    /// <param name="logMessage">The message that Discord sent us</param>
    private Task OnClientLog(LogMessage logMessage)
    {
        LogLevel logLevel = GetLogLevelFromDiscordLogSeverity(logMessage.Severity);
        logger.Log(logLevel, logMessage.Exception, logMessage.Message);
        return Task.CompletedTask;
    }

    /// <exclude/>
    /// <summary>
    /// Discord LogSeverity is slightly different from Microsoft's LogLevel
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if there is an unhandled LogSeverity</exception>
    private static LogLevel GetLogLevelFromDiscordLogSeverity(LogSeverity logSeverity)
    {
        return logSeverity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(logSeverity), logSeverity, null)
        };
    }
}
