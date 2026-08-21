using MediatR;
using NetCore.Donation.Application.PaymentSchedule.DTOs;

namespace NetCore.Donation.Application.PaymentSchedule.QueryPaymentSchedules;

public sealed record QueryPaymentSchedules(Guid? ContactId = null)
    : IRequest<IQueryable<QueryPaymentScheduleDto>>;