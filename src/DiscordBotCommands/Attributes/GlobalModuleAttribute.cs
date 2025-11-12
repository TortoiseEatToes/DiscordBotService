namespace DiscordBotCommands.Attributes;
using Discord.Interactions;

/// <summary>
/// Used to decorate <see cref="InteractionModuleBase"/> classes that should be applied globally to the bot.
/// </summary>
/// <remarks>
/// This type is also used to find the assembly where your discord commands are.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GlobalModuleAttribute : Attribute
{
}
