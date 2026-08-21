namespace NetCore.Donation.Migration.Common.Interface;

public interface IDataSeed
{
	IEnumerable<Type> Dependencies { get; }

	Task SeedAsync();
}
