using MediatR;

namespace NetCore.Donation.Application.Donation.QueueTransactionPending;

public sealed record QueueTransactionPendingCommand(Guid TransactionId) : IRequest<Guid>;
