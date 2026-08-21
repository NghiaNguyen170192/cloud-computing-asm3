using MediatR;

namespace NetCore.Donation.Application.Donation.CompleteDonationTransaction;

public sealed record CompleteDonationTransactionCommand(Guid TransactionId) : IRequest<Guid>;
