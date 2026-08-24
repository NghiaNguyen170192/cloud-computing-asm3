using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCore.Donation.Application.Donation.CompleteDonationTransaction;
using NetCore.Donation.Application.Extensions;
using NetCore.Donation.Infrastructure.Database.AppSettingConfigurations;
using NetCore.Donation.Infrastructure.Database.Extensions;
using NetCore.Donation.Infrastructure.Storage;
using NetCore.Donation.Migration;
using NetCore.Donation.Migration.Extensions;
using NetCore.Donation.Migration.Seeds.Base;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
var host = Host
	.CreateDefaultBuilder(args)
	.UseEnvironment(environment)
	.AddSharedConfiguration()
	.ConfigureServices((context, services) =>
	{
		var applicationConnectionString = context.Configuration.GetConnectionString("netcore-donation-db");
		if (!string.IsNullOrWhiteSpace(applicationConnectionString))
		{
			context.Configuration["Database:ApplicationConnectionString"] = applicationConnectionString;
		}

		var redisConnectionString = context.Configuration.GetConnectionString("redis");
		if (!string.IsNullOrWhiteSpace(redisConnectionString))
		{
			context.Configuration["Database:RedisConnectionString"] = redisConnectionString;
		}

		var databaseConfiguration = context.Configuration.GetSection("Database").Get<DatabaseConfiguration>() ?? new();
		services.AddApplication();
		services.AddSingleton<IDonationTransactionOutcome, RandomDonationTransactionOutcome>();
		services.AddInfrastructure(context.Configuration);
		services.AddObjectStorage(context.Configuration);
		services.AddMigrationService();
	})
	.AddLogger("netcore-migration-logs")
	.Build();

using var scope = host.Services.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<MigrationService>();
await service.RunAsync(args);