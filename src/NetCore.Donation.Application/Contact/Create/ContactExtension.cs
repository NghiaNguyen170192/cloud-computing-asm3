namespace NetCore.Donation.Application.Contact.Create;

public static class ContactExtension
{
    public static Domain.Entities.Contact ToDbEntity(this CreateContactCommand request)
    {
        return Domain.Entities.Contact.Create(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.AddressLine,
            request.Email,
            request.PhoneNumber,
            request.CountryId,
            request.Gender,
            request.DoNotEmail,
            request.DoNotSms);
    }
}
