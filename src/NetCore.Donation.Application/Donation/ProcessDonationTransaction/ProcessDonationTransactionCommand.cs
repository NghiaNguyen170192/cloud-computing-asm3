using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.Donation.ProcessDonationTransaction;

public sealed record ProcessDonationTransactionCommand(
    Guid PaymentScheduleId,
    Guid ContactId,
    Guid PaymentMethodId,
    decimal Amount,
    PaymentType PaymentType,
    bool IsRecurring,
    RecurringInterval RecurringInterval) : IRequest<Guid>;
