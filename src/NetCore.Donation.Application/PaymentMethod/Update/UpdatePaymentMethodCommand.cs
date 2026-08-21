using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.PaymentMethod.Update;

public sealed record UpdatePaymentMethodCommand(
    Guid Id,
    string DisplayName,
    PaymentType PaymentType = PaymentType.Bank) : IRequest<bool>;
