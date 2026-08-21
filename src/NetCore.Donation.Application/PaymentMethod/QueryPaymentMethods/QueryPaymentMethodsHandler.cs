using MediatR;
using NetCore.Donation.Application.PaymentMethod.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.PaymentMethod.QueryPaymentMethods;

public class QueryPaymentMethodsHandler(IPaymentMethodRepository paymentMethodRepository)
    : IRequestHandler<QueryPaymentMethods, IQueryable<QueryPaymentMethodDto>>
{
    public Task<IQueryable<QueryPaymentMethodDto>> Handle(
        QueryPaymentMethods request,
        CancellationToken cancellationToken)
    {
        var query = paymentMethodRepository.GetAll().ToQueryDto();
        if (request.ContactId is { } contactId)
        {
            query = query.Where(paymentMethod => paymentMethod.ContactId == contactId);
        }

        return Task.FromResult(query);
    }
}