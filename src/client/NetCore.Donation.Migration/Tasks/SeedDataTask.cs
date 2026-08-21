using NetCore.Donation.Migration.Common.Interface;
using Serilog;

namespace NetCore.Donation.Migration.Tasks;

public class SeedDataTask : IMigrationTask
{
	private readonly ILogger logger;
	private readonly IDataSeedRunner dataSeeds;
	private readonly string taskName;

	public SeedDataTask(ILogger logger, IDataSeedRunner dataSeeds)
	{
		this.logger = logger;
		this.dataSeeds = dataSeeds;
		taskName = GetType().FullName ?? string.Empty;
	}

	public IEnumerable<Type> Dependencies => new List<Type>()
	{
		typeof(ApplyPendingMigrationTask),
	};

	public async Task ExecuteAsync(string[] args)
	{
		logger.Information($"Start [{taskName}]");
		if (args.Contains("-s"))
        {
            await dataSeeds.RunSeedsAsync();
        }

		logger.Information($"End [{taskName}]");
	}
}