namespace NetCore.Donation.Infrastructure.Database.AppSettingConfigurations;

/// <summary>
/// Configuration options for distributed caching.
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Gets or sets the default time to live for cached items in minutes.
    /// </summary>
    public int DefaultTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the key prefix for cache entries.
    /// </summary>
    public string KeyPrefix { get; set; } = "netcore";

    /// <summary>
    /// Gets or sets the environment name to be used in cache keys.
    /// </summary>
    public string Environment { get; set; } = "dev";

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to use sliding expiration.
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = false;
}