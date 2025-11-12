using Microsoft.Extensions.DependencyInjection;
using SharedUtilities.Secrets;

namespace SharedUtilities.Extensions;

/// <summary>
/// Utility functions for required services
/// </summary>
public static class IServiceCollectionExtensions
{
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
}
