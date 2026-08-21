using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Application.Behaviors;
using NetCore.Donation.Application.Donation.CompleteDonationTransaction;

namespace NetCore.Donation.Application.Extensions;

public static class DependencyInjection
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		services.AddSingleton<IDonationTransactionOutcome, RandomDonationTransactionOutcome>();
		services.AddMediatR(cfg =>
		{
			cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly);
			// Add idempotency behavior before logging to catch duplicates early
			cfg.AddOpenBehavior(typeof(IdempotencyBehavior<,>));
			cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
		});

		return services;
	}
}