using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.PaymentMethod.Create;

public sealed record CreatePaymentMethodCommand(
    Guid ContactId,
    string DisplayName,
    PaymentType PaymentType = PaymentType.Bank) : IRequest<Guid>;
