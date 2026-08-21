using Npgsql;

namespace NetCore.Donation.Infrastructure.Database.Tests;

[TestClass]
public class MigrationTest
{
    private readonly ApplicationDatabaseContext context;
    private readonly IMigrationsAssembly migrationsAssembly;

    public MigrationTest()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDatabaseContext>();
        optionsBuilder.UseNpgsql(new NpgsqlConnection(), o => o.MigrationsAssembly("NetCore.Donation.Infrastructure.Database"));
        context = new ApplicationDatabaseContext(optionsBuilder.Options);
        migrationsAssembly = context.GetService<IMigrationsAssembly>();
    }

    [TestMethod]
    public void MigrationsAssemblyModelSnapshotNotNull()
    {
        Assert.IsNotNull(migrationsAssembly.ModelSnapshot);
        Assert.IsNotNull(migrationsAssembly.ModelSnapshot?.Model);
    }

    [TestMethod]
    public void ModelSnapshotHasNoDifferencesModel()
    {
        var modelSnapshot = context.GetService<IModelRuntimeInitializer>().Initialize(migrationsAssembly.ModelSnapshot?.Model!);
        var sourceModel = modelSnapshot.GetRelationalModel();
        var targetModel = context.GetService<IDesignTimeModel>().Model.GetRelationalModel();
        var hasDifferences = context.GetService<IMigrationsModelDiffer>().HasDifferences(sourceModel, targetModel);

        Assert.IsNotNull(sourceModel);
        Assert.IsNotNull(targetModel);
        Assert.IsFalse(hasDifferences);
    }
}