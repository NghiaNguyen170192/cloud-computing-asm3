using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.PaymentSchedule.Create;

public sealed record CreatePaymentScheduleCommand(
    Guid ContactId,
    Guid PaymentMethodId,
    decimal Amount,
    DateOnly BookDate,
    RecurringInterval RecurringInterval,
    PaymentType PaymentType = PaymentType.Bank) : IRequest<Guid>;
