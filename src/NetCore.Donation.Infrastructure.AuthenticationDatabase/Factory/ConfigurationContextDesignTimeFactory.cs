using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;

namespace NetCore.Donation.Infrastructure.AuthenticationDatabase.Factory
{
    public class ConfigurationContextDesignTimeFactory : DesignTimeDbContextFactoryBase<ConfigurationDbContext>
    {
        public ConfigurationContextDesignTimeFactory() : base("IdpConnectionString", typeof(AssemblyReference).GetTypeInfo().Assembly.GetName().Name)
        {
        }

        public ConfigurationContextDesignTimeFactory(string connectionStringName, string migrationsAssemblyName)
            : base(connectionStringName, migrationsAssemblyName)
        {
        }

        protected override ConfigurationDbContext CreateNewInstance(DbContextOptions<ConfigurationDbContext> options)
        {
            return new ConfigurationDbContext(options, new ConfigurationStoreOptions());
        }
    }
}
