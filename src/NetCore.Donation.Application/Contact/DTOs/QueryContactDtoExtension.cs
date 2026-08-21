namespace NetCore.Donation.Application.Contact.DTOs;

public static class QueryContactDtoExtension
{
    public static IQueryable<QueryContactDto> ToQueryDto(this IQueryable<Domain.Entities.Contact> contacts)
    {
        return contacts.Select(contact => new QueryContactDto
        {
            Id = contact.Id,
            FullName = contact.FirstName + " " + contact.LastName,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            DateOfBirth = contact.DateOfBirth,
            AddressLine = contact.AddressLine,
            Email = contact.Email,
            PhoneNumber = contact.PhoneNumber,
            Gender = contact.Gender,
            IsActive = contact.IsActive,
            DoNotEmail = contact.DoNotEmail,
            DoNotSms = contact.DoNotSms,
            CountryId = contact.CountryId,
            CountryName = contact.Country.Name,
        });
    }
}
