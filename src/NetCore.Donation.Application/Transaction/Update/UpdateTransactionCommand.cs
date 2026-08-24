using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.Transaction.Update;

public sealed record UpdateTransactionCommand(
    Guid Id,
    decimal Amount,
    Guid PaymentMethodId,
    PaymentType PaymentType,
    DateOnly BookDate,
    DateOnly ReceivedDate,
    Guid? PaymentScheduleId,
    TransactionStatus Status) : IRequest<bool>;