using System.Reflection;

namespace DiscordBotService.Secrets;

/// <summary>
/// Uses user secrets and enviornment variables as our secrets
/// </summary>
/// <remarks>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration">Configuration Basics Link</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets#manage-user-secrets-with-visual-studio">User Secrets Link</seealso>
/// </remarks>
public class SecretsManager : ISecretsManager
{
    /// <summary>
    /// Stores our secrets.
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Gets the <see cref="GetDefaultAssembly"/>
    /// </summary>
    public SecretsManager(ILogger<SecretsManager> logger) : this(logger, [GetDefaultAssembly()]) { }

    /// <summary>
    /// Creates the IConfiguration using UserSecrets (from the given assemblyList) and EnvironmentVariables.
    /// </summary>
    /// <param name="assemblyList">List of assemblies that have UserSecrets</param>
    public SecretsManager(ILogger<SecretsManager> logger, List<Assembly> assemblyList)
    {
        logger.LogTrace("Creating SecretsManager");
        _configuration = CreateConfiguration(logger, assemblyList);
    }

    ///<inheritdoc/>
    public string? GetSecret(string secretName)
    {
        return _configuration[secretName];
    }

    ///<inheritdoc/>
    public string GetRequiredSecret(string secretName)
    {
        return GetSecret(secretName) ?? throw new ArgumentNullException($"Failed to find secret with name:'{secretName}'");
    }

    /// <summary>
    /// Gets the entry assembly or the executing assembly if that fails.
    /// </summary>
    private static Assembly GetDefaultAssembly()
    {
        Assembly? assembly = Assembly.GetEntryAssembly();
        assembly ??= Assembly.GetExecutingAssembly();
        return assembly;
    }

    /// <summary>
    /// Creates the IConfiguration object that contains our secrets.
    /// </summary>
    /// <param name="assemblyList">The assemblies to get user secrets from.</param>
    /// <remarks>
    /// Environment variables will override local secrets that have the same names due to ordering in the constructor.
    /// </remarks>
    private static IConfiguration CreateConfiguration(ILogger<SecretsManager> logger, List<Assembly> assemblyList)
    {
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        foreach (Assembly assembly in assemblyList)
        {
            logger.LogDebug("Adding user secrets from assembly: {AssemblyName}", assembly.GetName());
            configurationBuilder = configurationBuilder.AddUserSecrets(assembly);
        }
        return configurationBuilder.AddEnvironmentVariables().Build();
    }
}
