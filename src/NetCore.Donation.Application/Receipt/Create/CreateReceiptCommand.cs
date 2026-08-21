using MediatR;

namespace NetCore.Donation.Application.Receipt.Create;

public sealed record CreateReceiptCommand(Guid ContactId, Guid? TransactionId = null) : IRequest<Guid>;