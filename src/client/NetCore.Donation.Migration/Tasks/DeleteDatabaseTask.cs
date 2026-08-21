using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Migration.Common.Interface;
using Serilog;

namespace NetCore.Donation.Migration.Tasks;

public class DeleteDatabaseTask : IMigrationTask
{
	private readonly ApplicationDatabaseContext context;
	private readonly ILogger logger;
	private readonly string taskName;

	public DeleteDatabaseTask(ApplicationDatabaseContext applicationDatabaseContext, ILogger logger)
	{
		this.logger = logger;
		context = applicationDatabaseContext;
		taskName = GetType().FullName ?? string.Empty;
	}

	public IEnumerable<Type> Dependencies => new List<Type>();

	public async Task ExecuteAsync(string[] args)
	{
		logger.Information($"Start {taskName}");

		if (!args.Contains("-d"))
		{
			logger.Information($"No command for running {taskName}");
			logger.Information($"End {taskName}");
			return;
		}

		if (!await context.Database.CanConnectAsync())
		{
			logger.Information("Cannot connect to database.");
			logger.Information($"End {taskName}");
			return;
		}

		logger.Information("Deleting Database");
		await context.Database.EnsureDeletedAsync();
		logger.Information($"End [{taskName}]");
	}
}
