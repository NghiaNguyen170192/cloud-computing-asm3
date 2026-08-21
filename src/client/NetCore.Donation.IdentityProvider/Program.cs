using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NetCore.Donation.Infrastructure.Database.Extensions;

namespace NetCore.Donation.IdentityProvider
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args)
        {
            return Host
              .CreateDefaultBuilder(args)
              .AddSharedConfiguration()
              .ConfigureAppConfiguration((hostbuilderContext, configurationBuilder) =>
                  configurationBuilder.AddAppSettings(hostbuilderContext, args))
              .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
              .AddLoggingConfiguration("netcore-idp");
        }
    }
}
