using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using NetCore.Donation.Infrastructure.Database.AppSettingConfigurations;
using Npgsql;

namespace NetCore.Donation.Infrastructure.Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDatabaseContext>
{
	public ApplicationDatabaseContext CreateDbContext(string[] args)
	{
		var npgsqlConnection = new NpgsqlConnection();
		var databaseConfiguration = Options.Create(new DatabaseConfiguration());
		var optionsBuilder = new DbContextOptionsBuilder<ApplicationDatabaseContext>();
		var migrationsAssembly = databaseConfiguration.Value.MigrationsAssembly ?? GetType().Assembly.FullName;

		optionsBuilder.UseNpgsql(npgsqlConnection, o => o.MigrationsAssembly(migrationsAssembly));

		// Dispatcher is null for design-time context (migrations don't dispatch domain events)
		return new ApplicationDatabaseContext(optionsBuilder.Options, null!);
	}
}