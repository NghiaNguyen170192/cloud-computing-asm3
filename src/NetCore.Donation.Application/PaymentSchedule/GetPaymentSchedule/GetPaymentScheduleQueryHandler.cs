using MediatR;
using NetCore.Donation.Application.PaymentSchedule.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.PaymentSchedule.GetPaymentSchedule;

public class GetPaymentScheduleQueryHandler(IPaymentScheduleRepository paymentScheduleRepository)
    : IRequestHandler<GetPaymentScheduleQuery, QueryPaymentScheduleDto?>
{
    public Task<QueryPaymentScheduleDto?> Handle(
        GetPaymentScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var paymentSchedule = paymentScheduleRepository.GetAll()
            .ToQueryDto()
            .FirstOrDefault(paymentSchedule => paymentSchedule.Id == request.Id);

        return Task.FromResult(paymentSchedule);
    }
}