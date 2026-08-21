using MediatR;
using NetCore.Donation.Application.PaymentMethod.DTOs;

namespace NetCore.Donation.Application.PaymentMethod.QueryPaymentMethods;

public sealed record QueryPaymentMethods(Guid? ContactId = null)
    : IRequest<IQueryable<QueryPaymentMethodDto>>;