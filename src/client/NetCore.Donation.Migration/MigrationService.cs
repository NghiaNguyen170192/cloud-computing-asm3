using NetCore.Donation.Migration.Common.Interface;
using NetCore.Donation.Migration.Extensions;
using Serilog;

namespace NetCore.Donation.Migration;

public class MigrationService
{
	private readonly ILogger logger;
	private readonly IEnumerable<IMigrationTask> sortedMigrationTasks;

	public MigrationService(ILogger logger, IEnumerable<IMigrationTask> migrationTasks)
	{
		this.logger = logger;
		sortedMigrationTasks = migrationTasks.TopologicalSort(x => x.Dependencies).ToList();
	}

	public async Task RunAsync(string[] args)
	{
		logger.Information($"Start with args: {string.Join(" ", args, 0, args.Length)}");
		try
		{
			foreach (var migrationTask in sortedMigrationTasks)
			{
				logger.Information("-----------------------------------");
				await migrationTask.ExecuteAsync(args);
				logger.Information("-----------------------------------");
			}
		}
		catch (Exception exception)
		{
			logger.Error(exception.Message);
			logger.Error(exception.StackTrace!);
		}

		logger.Information("End");
	}
}