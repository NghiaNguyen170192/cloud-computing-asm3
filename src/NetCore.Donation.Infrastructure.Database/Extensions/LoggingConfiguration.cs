using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace NetCore.Donation.Infrastructure.Database.Extensions;

public static class LoggingConfiguration
{
	// Template that includes correlation ID when available
	private const string DefaultFileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message:lj}{NewLine:l}{Exception:l}";
	private const string CorrelationIdFileOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] [CorrelationId: {CorrelationId}] {Message:lj}{NewLine:l}{Exception:l}";
	private static readonly MessageTemplateTextFormatter Formatter = new(DefaultFileOutputTemplate, CultureInfo.InvariantCulture);
	private static readonly MessageTemplateTextFormatter FormatterWithCorrelation = new(CorrelationIdFileOutputTemplate, CultureInfo.InvariantCulture);

	public static IServiceCollection AddNewLogger(this IServiceCollection services, string applicationName)
	{

		return services;
	}

	public static IHostBuilder AddLogger(this IHostBuilder hostBuilder, string applicationName)
	{
		return hostBuilder.UseSerilog((hostBuilderContext, loggerConfiguration) =>
		{
			// Enrich logs with LogContext properties (including CorrelationId from MediatR behaviors)
			loggerConfiguration
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Application", applicationName)
				.WriteTo.Async(loggerSinkConfiguration =>
				{
					var logLevel = hostBuilderContext.Configuration.GetValue<LogEventLevel>("Logging:LogLevel:Default");
					var logFileDirectory = hostBuilderContext.Configuration.GetValue<string>("Logging:Path");
					var logFileName = $"{DateTime.Now:yyyy-MM-dd}.log";
					var logFilePath = Path.Combine(logFileDirectory, applicationName, logFileName);

					// Use formatter with correlation ID to include it in logs
					loggerSinkConfiguration.File(
						formatter: FormatterWithCorrelation,
						path: logFilePath,
						restrictedToMinimumLevel: logLevel,
						rollingInterval: RollingInterval.Day,
						shared: true);

					// Console output with correlation ID
					loggerSinkConfiguration.Console(logLevel, CorrelationIdFileOutputTemplate);
				});
		});
	}
}