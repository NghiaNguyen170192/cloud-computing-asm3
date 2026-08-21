#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace NetCore.Donation.Infrastructure.Database.Extensions;

public static class AppSettingsConfiguration
{
    public static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder configurationBuilder, HostBuilderContext context, string[] args)
    {
        return configurationBuilder.AddAppSettings(context.HostingEnvironment.EnvironmentName, args, context.HostingEnvironment.IsDevelopment());
    }

    public static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder configurationBuilder, string environment, string[] args)
    {
        var isDevelopment = string.Equals(environment, Environments.Development, StringComparison.OrdinalIgnoreCase);
        return configurationBuilder.AddAppSettings(environment, args, isDevelopment);
    }

    private static IConfigurationBuilder AddAppSettings(this IConfigurationBuilder configurationBuilder, string environment, string[] args, bool isDevelopment)
    {
        configurationBuilder.SetBasePath(Directory.GetCurrentDirectory());

        var serviceDefaultsPath = ResolveServiceDefaultsPath(isDevelopment);
        if (!string.IsNullOrWhiteSpace(serviceDefaultsPath))
        {
            var sharedConfigPath = Path.Combine(serviceDefaultsPath, "appsettings.json");
            if (File.Exists(sharedConfigPath))
            {
                configurationBuilder.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);
            }

            var sharedEnvConfigPath = Path.Combine(serviceDefaultsPath, $"appsettings.{environment}.json");
            if (File.Exists(sharedEnvConfigPath))
            {
                configurationBuilder.AddJsonFile(sharedEnvConfigPath, optional: true, reloadOnChange: true);
            }
        }

        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        configurationBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCommandLine(args);

        return configurationBuilder;
    }

    private static string? ResolveServiceDefaultsPath(bool isDevelopment)
    {
        if (!isDevelopment)
        {
            var containerPath = Path.Combine("/app", "shared-config");
            if (Directory.Exists(containerPath))
            {
                return containerPath;
            }
        }

        var basePath = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(basePath, "NetCore.Donation.ServiceDefaults"),
            Path.Combine(basePath, "..", "NetCore.Donation.ServiceDefaults"),
            Path.Combine(basePath, "..", "NetCore.Donation", "NetCore.Donation.ServiceDefaults"),
            Path.Combine(basePath, "..", "..", "NetCore.Donation", "NetCore.Donation.ServiceDefaults"),
            Path.Combine(basePath, "..", "..", "client", "NetCore.Donation", "NetCore.Donation.ServiceDefaults"),
            Path.Combine(basePath, "..", "..", "..", "client", "NetCore.Donation", "NetCore.Donation.ServiceDefaults"),
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}