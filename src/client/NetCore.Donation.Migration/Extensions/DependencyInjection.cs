using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Migration.Common;
using NetCore.Donation.Migration.Common.Interface;
using System.Reflection;

namespace NetCore.Donation.Migration.Extensions;

public static class DependencyInjection
{
	public static void AddMigrationService(this IServiceCollection services)
	{
		services.RegisterClassesFromAssemblyInterface<IMigrationTask>();
		services.AddScoped<MigrationService>();

		services.RegisterClassesFromAssemblyInterface<IDataSeed>();
		services.AddScoped<IDataSeedRunner, DataSeedRunner<IDataSeed>>();
	}

	public static void RegisterAllTypes<T>(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Transient)
	{
		var typesFromAssemblies = assemblies.SelectMany(a => a.DefinedTypes.Where(x => x.GetInterfaces().Contains(typeof(T))));
		foreach (var type in typesFromAssemblies)
        {
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
        }
    }

	private static void RegisterClassesFromAssemblyInterface<T>(this IServiceCollection services)
	{
		services.Scan(selector => selector
			.FromAssemblyOf<T>()
			.AddClasses(classes => classes
				.AssignableTo<T>())
			.AsImplementedInterfaces()
			.WithScopedLifetime());
	}
}