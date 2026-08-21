using MediatR;
using NetCore.Donation.Application.PaymentSchedule.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.PaymentSchedule.QueryPaymentSchedules;

public class QueryPaymentSchedulesHandler(IPaymentScheduleRepository paymentScheduleRepository)
    : IRequestHandler<QueryPaymentSchedules, IQueryable<QueryPaymentScheduleDto>>
{
    public Task<IQueryable<QueryPaymentScheduleDto>> Handle(
        QueryPaymentSchedules request,
        CancellationToken cancellationToken)
    {
        var query = paymentScheduleRepository.GetAll().ToQueryDto();
        if (request.ContactId is { } contactId)
        {
            query = query.Where(paymentSchedule => paymentSchedule.ContactId == contactId);
        }

        return Task.FromResult(query);
    }
}