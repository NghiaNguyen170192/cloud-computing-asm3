namespace NetCore.Donation.ServiceDefaults.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ApplicationConnectionString { get; set; } = string.Empty;

    public string IdpConnectionString { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string MigrationsAssembly { get; set; } = string.Empty;

    public string RedisConnectionString { get; set; } = string.Empty;
}

public class AuthenticationServerOptions
{
    public const string SectionName = "AuthenticationServer";

    public string Audience { get; set; } = string.Empty;

    public string CertificatePassword { get; set; } = string.Empty;

    public string CertificatePath { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}

public class LoggingOptions
{
    public const string SectionName = "Logging";

    public string Path { get; set; } = string.Empty;

    public Dictionary<string, string> LogLevel { get; set; } = new();
}

public class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";

    public string ServiceName { get; set; } = "NetCore.Donation";

    public string ServiceVersion { get; set; } = "1.0.0";
}

public class ApplicationOptions
{
    public string AllowedHosts { get; set; } = "*";

    public bool DetailedErrors { get; set; } = false;
}