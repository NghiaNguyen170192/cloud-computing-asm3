namespace NetCore.Donation.Application.PaymentMethod.DTOs;

public static class QueryPaymentMethodDtoExtension
{
    public static IQueryable<QueryPaymentMethodDto> ToQueryDto(
        this IQueryable<Domain.Entities.PaymentMethod> paymentMethods)
    {
        return paymentMethods.Select(paymentMethod => new QueryPaymentMethodDto
        {
            Id = paymentMethod.Id,
            ContactId = paymentMethod.ContactId,
            ContactFullName = paymentMethod.Contact.FirstName + " " + paymentMethod.Contact.LastName,
            DisplayName = paymentMethod.DisplayName,
            PaymentType = paymentMethod.PaymentType,
        });
    }
}
