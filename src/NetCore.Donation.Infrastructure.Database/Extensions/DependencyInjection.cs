using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.Messaging;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.AppSettingConfigurations;
using NetCore.Donation.Infrastructure.Database.Messaging;
using NetCore.Donation.Infrastructure.Database.Repositories;
using Redis.OM;

namespace NetCore.Donation.Infrastructure.Database.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
	{
		var databaseConfiguration = new DatabaseConfiguration();
		configuration.GetSection("Database").Bind(databaseConfiguration);

		// Aspire WithReference injects ConnectionStrings__{resourceName}. Prefer those over local appsettings.
		var aspireApplicationConnectionString = configuration.GetConnectionString("netcore-donation-db");
		if (!string.IsNullOrWhiteSpace(aspireApplicationConnectionString))
		{
			databaseConfiguration.ApplicationConnectionString = aspireApplicationConnectionString;
		}

		var aspireRedisConnectionString = configuration.GetConnectionString("redis");
		if (!string.IsNullOrWhiteSpace(aspireRedisConnectionString))
		{
			databaseConfiguration.RedisConnectionString = aspireRedisConnectionString;
		}

		services.AddDbContext<ApplicationDatabaseContext>(builder =>
		{
			builder.UseNpgsql(
				databaseConfiguration.ApplicationConnectionString,
				optionsBuilder =>
				{
					optionsBuilder.MigrationsAssembly(databaseConfiguration.MigrationsAssembly);
					optionsBuilder.EnableRetryOnFailure();
				});
		});

		services.AddScoped<ICountryRepository, CountryRepository>();
		services.AddScoped<IContactRepository, ContactRepository>();
		services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
		services.AddScoped<IPaymentScheduleRepository, PaymentScheduleRepository>();
		services.AddScoped<ITransactionRepository, TransactionRepository>();
		services.AddScoped<IReceiptRepository, ReceiptRepository>();
		services.AddScoped<IJournalRepository, JournalRepository>();
		services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
		services.AddSingleton<RecordingIntegrationEventPublisher>();
		services.AddSingleton<IIntegrationEventPublisher>(sp => sp.GetRequiredService<RecordingIntegrationEventPublisher>());
		services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDatabaseContext>());

		// Cache configuration
		services.Configure<CacheConfiguration>(configuration.GetSection("CacheConfiguration"));

		// Distributed caching with Redis
		// Attempt to register Redis provider; fallback to in-memory cache if Redis types are not available
		try
		{
			services.AddSingleton(new RedisConnectionProvider(databaseConfiguration.RedisConnectionString));
			services.AddScoped(typeof(ICacheRepository<>), typeof(DistributedCacheRepository<>));
		}
		catch
		{
			// If Redis.OM can't be initialized (e.g., missing DocumentAttribute on entities),
			// use in-memory cache implementation as a safe fallback for local development.
			services.AddScoped(typeof(ICacheRepository<>), typeof(InMemoryCacheRepository<>));
		}

		return services;
	}
}