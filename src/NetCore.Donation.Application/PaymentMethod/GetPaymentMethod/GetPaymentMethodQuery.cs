using MediatR;
using NetCore.Donation.Application.PaymentMethod.DTOs;

namespace NetCore.Donation.Application.PaymentMethod.GetPaymentMethod;

public sealed record GetPaymentMethodQuery(Guid Id) : IRequest<QueryPaymentMethodDto?>;