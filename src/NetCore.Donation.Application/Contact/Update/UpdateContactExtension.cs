namespace NetCore.Donation.Application.Contact.Update;

public static class UpdateContactExtension
{
    public static void UpdateEntity(this UpdateContactCommand request, Domain.Entities.Contact contact)
    {
        contact.UpdateDetails(
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
