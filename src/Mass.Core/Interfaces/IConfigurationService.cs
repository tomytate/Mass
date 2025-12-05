namespace Mass.Core.Interfaces;

/// <summary>
/// Public facade for configuration management.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="fallback">The fallback value if the key is not found.</param>
    /// <returns>The configuration value or fallback.</returns>
    T Get<T>(string key, T fallback = default!);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value to set.</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Loads configuration from persistent storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves configuration to persistent storage.
    /// </summary>
    Task SaveAsync();
}
