using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;

namespace NetCore.Donation.Infrastructure.AuthenticationDatabase.Factory
{
    public class PersistedGrantContextDesignTimeFactory : DesignTimeDbContextFactoryBase<PersistedGrantDbContext>
    {
        public PersistedGrantContextDesignTimeFactory() : base("IdpConnectionString", typeof(AssemblyReference).GetTypeInfo().Assembly.GetName().Name)
        {
        }

        protected override PersistedGrantDbContext CreateNewInstance(DbContextOptions<PersistedGrantDbContext> options)
        {
            return new PersistedGrantDbContext(options, new OperationalStoreOptions());
        }
    }
}
