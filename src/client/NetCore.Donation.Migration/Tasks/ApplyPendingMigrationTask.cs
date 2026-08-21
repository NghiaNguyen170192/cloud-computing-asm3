using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Migration.Common.Interface;
using Serilog;

namespace NetCore.Donation.Migration.Tasks;

public class ApplyPendingMigrationTask : IMigrationTask
{
	private readonly ApplicationDatabaseContext context;
	private readonly ILogger logger;
	private readonly string taskName;

	public ApplyPendingMigrationTask(ApplicationDatabaseContext applicationDatabaseContext, ILogger logger)
	{
		this.logger = logger;
		context = applicationDatabaseContext;
		taskName = GetType().FullName ?? string.Empty;
	}

	public IEnumerable<Type> Dependencies => new List<Type>()
	{
		typeof(DeleteDatabaseTask)
	};

	public async Task ExecuteAsync(string[] args)
	{
		logger.Information($"Start: {taskName}");
		if (!args.Contains("-m"))
		{
			logger.Information($"No command for running {taskName}");
			logger.Information($"End: {taskName}]");
			return;
		}

		var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
		if (!pendingMigrations.Any())
		{
			logger.Information($"No command for running {taskName}");
			logger.Information($"End: {taskName}]");
			return;
		}

		logger.Information("Applying Migration");
		await context.Database.MigrateAsync();

		logger.Information($"End {taskName}");
	}
}
