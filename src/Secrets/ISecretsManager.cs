namespace DiscordBotService.Secrets;

/// <summary>
/// A secrets manager is our generic tool for accessing secrets in both development and production environments.
/// </summary>
public interface ISecretsManager
{
    /// <summary>
    /// Gets a secret with a given name.
    /// </summary>
    /// <param name="secretName">The secret name</param>
    /// <returns>The secret value or null if nothing was found</returns>
    string? GetSecret(string secretName);

    /// <summary>
    /// Gets a secret with a given name.  Throws an exception if one was not found.
    /// </summary>
    /// <param name="secretName">The secret name</param>
    /// <returns>The secret value</returns>
    /// <exception cref="ArgumentNullException">Throws an exception if the secret was not found</exception>
    string GetRequiredSecret(string secretName);
}
