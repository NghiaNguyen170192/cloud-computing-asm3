using MediatR;

namespace NetCore.Donation.Application.Receipt.Delete;

public sealed record DeleteReceiptCommand(Guid Id) : IRequest<bool>;