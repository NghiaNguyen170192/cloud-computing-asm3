using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Application.Behaviors;
using NetCore.Donation.Application.Donation.CompleteDonationTransaction;

namespace NetCore.Donation.Application.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddSingleton<IDonationTransactionOutcome, SucceededDonationTransactionOutcome>();
		services.AddMediatR(cfg =>
		{
			cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly);
			cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
		});

		return services;
	}
}
