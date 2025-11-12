namespace DiscordBotService.Discord;

/// <summary>
/// Used to decorate <see cref="InteractionModuleBase"/> classes that should be applied globally to the bot.
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class |
                       System.AttributeTargets.Struct,
                       AllowMultiple = false)
]
public class GlobalModuleAttribute : System.Attribute
{
}
