using Discord.Interactions;

namespace DiscordBotService.Discord;

/// <summary>
/// Convenience wrapper for tracking the different kinds of modules
/// </summary>
internal struct DiscordModules
{
    public ModuleInfo[] Global;
    public ModuleInfo[] Guild;
}