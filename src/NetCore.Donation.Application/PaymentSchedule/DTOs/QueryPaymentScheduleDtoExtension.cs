namespace NetCore.Donation.Application.PaymentSchedule.DTOs;

public static class QueryPaymentScheduleDtoExtension
{
    public static IQueryable<QueryPaymentScheduleDto> ToQueryDto(
        this IQueryable<Domain.Entities.PaymentSchedule> paymentSchedules)
    {
        return paymentSchedules.Select(paymentSchedule => new QueryPaymentScheduleDto
        {
            Id = paymentSchedule.Id,
            Identifier = paymentSchedule.Identifier,
            ContactId = paymentSchedule.ContactId,
            ContactFullName = paymentSchedule.Contact.FirstName + " " + paymentSchedule.Contact.LastName,
            PaymentMethodId = paymentSchedule.PaymentMethodId,
            PaymentMethodDisplayName = paymentSchedule.PaymentMethod.DisplayName,
            Amount = paymentSchedule.Amount,
            BookDate = paymentSchedule.BookDate,
            PaymentType = paymentSchedule.PaymentType,
            RecurringInterval = paymentSchedule.RecurringInterval,
            IsRecurring = true,
        });
    }
}
