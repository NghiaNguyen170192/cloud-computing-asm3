using MediatR;
using NetCore.Donation.Application.PaymentMethod.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.PaymentMethod.GetPaymentMethod;

public class GetPaymentMethodQueryHandler(IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<GetPaymentMethodQuery, QueryPaymentMethodDto?>
{
    public Task<QueryPaymentMethodDto?> Handle(
        GetPaymentMethodQuery request,
        CancellationToken cancellationToken)
    {
        var paymentMethod = paymentMethodRepository.GetAll()
            .ToQueryDto()
            .FirstOrDefault(paymentMethod => paymentMethod.Id == request.Id);

        return Task.FromResult(paymentMethod);
    }
}