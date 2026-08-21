using MediatR;
using NetCore.Donation.Application.PaymentSchedule.DTOs;

namespace NetCore.Donation.Application.PaymentSchedule.GetPaymentSchedule;

public sealed record GetPaymentScheduleQuery(Guid Id) : IRequest<QueryPaymentScheduleDto?>;