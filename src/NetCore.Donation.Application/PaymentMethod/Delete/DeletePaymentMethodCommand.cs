using MediatR;

namespace NetCore.Donation.Application.PaymentMethod.Delete;

public sealed record DeletePaymentMethodCommand(Guid Id) : IRequest<bool>;