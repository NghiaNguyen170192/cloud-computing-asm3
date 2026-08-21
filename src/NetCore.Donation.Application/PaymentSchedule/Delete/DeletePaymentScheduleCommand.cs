using MediatR;

namespace NetCore.Donation.Application.PaymentSchedule.Delete;

public sealed record DeletePaymentScheduleCommand(Guid Id) : IRequest<bool>;