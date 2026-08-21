using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetCore.Donation.ServiceDefaults.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // Load shared configuration first
        builder.AddSharedConfiguration();

        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder AddSharedConfiguration(this IHostApplicationBuilder builder)
    {
        AddSharedConfigurationSources(builder.Configuration, builder.Environment);

        RegisterConfigurationOptions(builder.Services, builder.Configuration);

        return builder;
    }

    public static IHostBuilder AddSharedConfiguration(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            AddSharedConfigurationSources(configurationBuilder, context.HostingEnvironment);
        });

        hostBuilder.ConfigureServices((context, services) =>
        {
            RegisterConfigurationOptions(services, context.Configuration);
        });

        return hostBuilder;
    }

    private static void AddSharedConfigurationSources(IConfigurationBuilder configurationBuilder, IHostEnvironment environment)
    {
        // Determine the ServiceDefaults path based on environment
        string serviceDefaultsPath;
        if (environment.IsDevelopment())
        {
            // Development: Navigate relative path from bin folder
            serviceDefaultsPath = Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "NetCore.Donation", "NetCore.Donation.ServiceDefaults"
            );
        }
        else
        {
            // Docker/Production: Use shared config from embedded resources or volume mount
            serviceDefaultsPath = Path.Combine("/app", "shared-config");

            // If shared-config doesn't exist, try embedded resources
            if (!Directory.Exists(serviceDefaultsPath))
            {
                LoadEmbeddedConfiguration(configurationBuilder, environment.EnvironmentName);
                return;
            }
        }

        // Normalize the path
        serviceDefaultsPath = Path.GetFullPath(serviceDefaultsPath);

        var sharedConfigPath = Path.Combine(serviceDefaultsPath, "appsettings.json");
        if (File.Exists(sharedConfigPath))
        {
            configurationBuilder.Sources.Insert(0,
                new Configuration.Json.JsonConfigurationSource
                {
                    Path = sharedConfigPath,
                    Optional = true,
                    ReloadOnChange = true,
                });
        }

        var sharedEnvConfigPath = Path.Combine(serviceDefaultsPath, $"appsettings.{environment.EnvironmentName}.json");
        if (File.Exists(sharedEnvConfigPath))
        {
            configurationBuilder.Sources.Insert(1,
                new Configuration.Json.JsonConfigurationSource
                {
                    Path = sharedEnvConfigPath,
                    Optional = true,
                    ReloadOnChange = true,
                });
        }
    }

    private static void RegisterConfigurationOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuthenticationServerOptions>(configuration.GetSection(AuthenticationServerOptions.SectionName));
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));
        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
        services.Configure<ApplicationOptions>(configuration);
    }

    private static void LoadEmbeddedConfiguration(IConfigurationBuilder configurationBuilder, string environmentName)
    {
        // Load configuration from embedded resources when file system access is not available
        var assembly = typeof(Extensions).Assembly;
        var resourcePrefix = "NetCore.Donation.ServiceDefaults.";

        // Load base appsettings.json
        var baseConfigResource = $"{resourcePrefix}appsettings.json";
        using (var stream = assembly.GetManifestResourceStream(baseConfigResource))
        {
            if (stream != null)
            {
                configurationBuilder.AddJsonStream(stream);
            }
        }

        // Load environment-specific appsettings
        var envConfigResource = $"{resourcePrefix}appsettings.{environmentName}.json";
        using (var stream = assembly.GetManifestResourceStream(envConfigResource))
        {
            if (stream != null)
            {
                configurationBuilder.AddJsonStream(stream);
            }
        }
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        // Get OpenTelemetry options from configuration
        var otelOptions = builder.Configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>() 
            ?? new OpenTelemetryOptions();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddBuiltInMeters();
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // We want to view all traces in development
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation()
                       .AddGrpcClientInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // builder.Services.AddOpenTelemetry()
        //    .WithMetrics(metrics => metrics.AddPrometheusExporter());

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.Exporter package)
        // builder.Services.AddOpenTelemetry()
        //    .UseAzureMonitor();
        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()

            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Skip HTTPS redirect on Lambda / HTTP-only hosts (API Gateway terminates TLS).
        var onLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
        if (!onLambda && app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // app.UseAuthentication();
        app.UseRouting();

        // app.UseAuthorization();
        app.MapControllers();

        // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        // app.MapPrometheusScrapingEndpoint();

        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
        });

        return app;
    }

    private static MeterProviderBuilder AddBuiltInMeters(this MeterProviderBuilder meterProviderBuilder) =>
        meterProviderBuilder.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http");
}