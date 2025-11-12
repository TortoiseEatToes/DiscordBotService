using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotCommands.Attributes;
using System.Collections.Concurrent;
using System.Reflection;

namespace DiscordBotService.Discord;

/// <summary>
/// Manages basic callbacks for interacting with the Discord API
/// </summary>
/// <param name="logger">Logger in the DiscordBot category</param>
/// <param name="discordSocketClient">Discord SocketClient for interacting with the bot</param>
/// <param name="interactionService">Interaction for callbacks for the bot</param>
/// <param name="serviceProvider">Used by discord functions that require it</param>
public class DiscordBot(
    ILogger<DiscordBot> logger,
    DiscordSocketClient discordSocketClient,
    InteractionService interactionService,
    IServiceProvider serviceProvider) : IDiscordBot, IAsyncDisposable
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

        logger.LogDebug("DiscordBot Setting callbacks");
        discordSocketClient.Ready += OnClientIsReadyAsync;
        discordSocketClient.Log += OnClientLog;
        discordSocketClient.JoinedGuild += OnJoinedGuild;
        discordSocketClient.InteractionCreated += OnInteractionCreatedAsync;

        AddInteractionServiceCallbacks();

        // Add your own GuildInvites callbacks here
        //discordSocketClient.InviteCreated;
        //discordSocketClient.IntegrationDeleted;

        // Add your own GuildScheduledEvents callbacks here
        //discordSocketClient.GuildScheduledEventCreated;
        //discordSocketClient.GuildScheduledEventUpdated;
        //discordSocketClient.GuildScheduledEventCancelled;
        //discordSocketClient.GuildScheduledEventCompleted;
        //discordSocketClient.GuildScheduledEventStarted;
        //discordSocketClient.GuildScheduledEventUserAdd;
        //discordSocketClient.GuildScheduledEventUserRemove;

        logger.LogDebug("DiscordBot logging in");
        await discordSocketClient.LoginAsync(TokenType.Bot, discordToken);
        await discordSocketClient.StartAsync();
        logger.LogDebug("DiscordBot logging in complete");
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

    /// <summary>
    /// When we receive the OnClientIsReady event from Discord, create the modules on all of our servers
    /// </summary>
    private async Task OnClientIsReadyAsync()
    {
        await AddModulesToGuildsAsync();
        logger.LogDebug("DiscordBot is ready");
    }

    /// <summary>
    /// Adds all global and guild based modules
    /// </summary>
    private async Task AddModulesToGuildsAsync()
    {
        DiscordModules discordModules = await GetDiscordModulesAsync();
        logger.LogDebug("Setting up global commands");
        var test = await interactionService.AddModulesGloballyAsync(deleteMissing: true, discordModules.Global);
        foreach (var intance in test)
        {
            logger.LogTrace($"Added global module: {intance.Name}");
        }
        logger.LogDebug("Finished setting up global commands");

        logger.LogDebug("Setting up guild commands");
        foreach (var guild in discordSocketClient.Guilds)
        {
            logger.LogTrace($"registering modules to {guild.Name}");
            await interactionService.AddModulesToGuildAsync(guild.Id, deleteMissing: true, discordModules.Guild);
        }
        logger.LogDebug("Finished setting up all commands");
    }

    /// <summary>
    /// Convenience wrapper for tracking the different kinds of modules
    /// </summary>
    private struct DiscordModules
    {
        public ModuleInfo[] Global;
        public ModuleInfo[] Guild;
    }

    /// <summary>
    /// Gets all of the modules from our assembly
    /// </summary>
    /// <remarks>
    /// Finds the assembly based on where <see cref="GlobalModuleAttribute"/> exists
    /// </remarks>
    private async Task<DiscordModules> GetDiscordModulesAsync()
    {
        List<ModuleInfo> globalModules = [];
        List<ModuleInfo> guildModules = [];

        var commandAssembly = typeof(GlobalModuleAttribute).Assembly;
        IEnumerable<ModuleInfo> allModules = await interactionService.AddModulesAsync(commandAssembly, serviceProvider);
        foreach (ModuleInfo module in allModules)
        {
            if (IsModuleGlobal(module))
            {
                globalModules.Add(module);
            }
            else
            {
                guildModules.Add(module);
            }
        }

        return new DiscordModules {
            Global = globalModules.ToArray(),
            Guild = guildModules.ToArray()
        };
    }

    /// <summary>
    /// Checks for the <see cref="GlobalModuleAttribute"/> to check if a module is global.
    /// </summary>
    private static bool IsModuleGlobal(ModuleInfo moduleInfo)
    {
        foreach (var attribute in moduleInfo.Attributes)
        {
            if (attribute.GetType() == typeof(GlobalModuleAttribute))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// When this bot joins a new guild, add our commands to that guild.
    /// </summary>
    private Task OnJoinedGuild(SocketGuild socketGuild)
    {
        logger.LogDebug($"OnJoinedGuild: {socketGuild.Id}");
        interactionService.AddCommandsToGuildAsync(socketGuild.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Callback for when an interaction is created
    /// </summary>
    private async Task OnInteractionCreatedAsync(SocketInteraction socketInteraction)
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

    /// <summary>
    /// Binds a <see cref="HandleInteractionExecuted{TInteraction}(TInteraction, IInteractionContext, IResult)"/> to all the Discord callbacks
    /// </summary>
    private void AddInteractionServiceCallbacks()
    {
        interactionService.SlashCommandExecuted += async (slashCommandInfo, interactionContext, result) => { await HandleInteractionExecutedAsync(slashCommandInfo, interactionContext, result); };
        interactionService.ContextCommandExecuted += async (contextCommandInfo, interactionContext, result) => { await HandleInteractionExecutedAsync(contextCommandInfo, interactionContext, result); };
        interactionService.ComponentCommandExecuted += async (componentCommandInfo, interactionContext, result) => { await HandleInteractionExecutedAsync(componentCommandInfo, interactionContext, result); };
        interactionService.AutocompleteCommandExecuted += async (autoCompleteCommandInfo, interactionContext, result) => { await HandleInteractionExecutedAsync(autoCompleteCommandInfo, interactionContext, result); };
        interactionService.AutocompleteHandlerExecuted += async (autoCompleteHandler, interactionContext, result) => { await HandleInteractionExecutedAsync(autoCompleteHandler, interactionContext, result); };
        interactionService.ModalCommandExecuted += async (modalCommandInfo, interactionContext, result) => { await HandleInteractionExecutedAsync(modalCommandInfo, interactionContext, result); };
    }

    /// <summary>
    /// Generic callback for logging information about issues with Discord interactions
    /// </summary>
    private async Task HandleInteractionExecutedAsync<TInteraction>(TInteraction interaction, IInteractionContext interactionContext, IResult result)
    {
        if (result.IsSuccess)
        {
            logger.LogDebug("Interaction success: {0}:{1}", GetInteractionContextString(interactionContext), interaction);
            return;
        }
        string interactionName = interaction is null ? $"Unknown {nameof(InteractionType)}" : $"{interaction}";
        await HandleInteractionErrorAsync(interactionName, interactionContext, result);
    }

    /// <summary>
    /// When we receive failed interactions, log out the information and try to respond if possible.
    /// </summary>
    private async Task HandleInteractionErrorAsync(string interactionName, IInteractionContext interactionContext, IResult result)
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

    /// <summary>
    /// Converts an interaction to a convenient string
    /// </summary>
    private static string GetInteractionContextString(IInteractionContext interactionContext)
    {
        return $"{interactionContext.Guild}:{interactionContext.User}";
    }

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
