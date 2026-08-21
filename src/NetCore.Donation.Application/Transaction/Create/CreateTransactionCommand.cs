using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.Transaction.Create;

public sealed record CreateTransactionCommand(
    decimal Amount,
    Guid? PaymentScheduleId,
    Guid ContactId,
    Guid PaymentMethodId,
    PaymentType PaymentType,
    DateOnly BookDate,
    DateOnly ReceivedDate) : IRequest<Guid>;
