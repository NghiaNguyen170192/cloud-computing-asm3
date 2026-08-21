using MediatR;

namespace NetCore.Donation.Application.Transaction.Delete;

public sealed record DeleteTransactionCommand(Guid Id) : IRequest<bool>;