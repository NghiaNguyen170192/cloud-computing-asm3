using MediatR;

namespace NetCore.Donation.Application.Receipt.Update;

public sealed record UpdateReceiptCommand(Guid Id, Guid? TransactionId) : IRequest<bool>;