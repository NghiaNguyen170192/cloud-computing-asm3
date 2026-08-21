using NetCore.Donation.Migration.Common.Interface;
using NetCore.Donation.Migration.Extensions;
using Serilog;

namespace NetCore.Donation.Migration.Common;

public class DataSeedRunner<T> : IDataSeedRunner
		where T : IDataSeed
{
	private readonly IEnumerable<T> sortedSeeds;
	private readonly ILogger logger;

	public DataSeedRunner(IEnumerable<T> seeds, ILogger logger)
	{
		sortedSeeds = seeds.TopologicalSort(x => x.Dependencies).ToList();
		this.logger = logger;
	}

	public async Task RunSeedsAsync()
	{
		var seedName = "";

		try
		{
			foreach (var seed in sortedSeeds)
			{
				seedName = seed.GetType().Name;
				await seed.SeedAsync();
			}
		}
		catch (Exception ex)
		{
			logger.Error(seedName);
			logger.Error(ex.Message);
			logger.Error(ex.StackTrace!);
			throw;
		}
	}
}