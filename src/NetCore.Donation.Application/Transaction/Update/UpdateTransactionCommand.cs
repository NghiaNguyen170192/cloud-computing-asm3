using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.Transaction.Update;

public sealed record UpdateTransactionCommand(
    Guid Id,
    decimal Amount,
    PaymentType PaymentType,
    DateOnly ReceivedDate) : IRequest<bool>;