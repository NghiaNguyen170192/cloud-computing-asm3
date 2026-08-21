#nullable disable

using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Entities;

public class Contact : Entity, IAggregateRoot
{
    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public string AddressLine { get; private set; }

    public string Email { get; private set; }

    public string PhoneNumber { get; private set; }

    public Gender Gender { get; private set; }

    public bool IsActive { get; private set; }

    public bool DoNotEmail { get; private set; }

    public bool DoNotSms { get; private set; }

    public Guid CountryId { get; private set; }

    public Country Country { get; private set; }

    public static Contact Create(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string addressLine,
        string email,
        string phoneNumber,
        Guid countryId,
        Gender gender = Gender.Other,
        bool doNotEmail = false,
        bool doNotSms = false)
    {
        Validate(firstName, lastName, dateOfBirth, addressLine, email, phoneNumber, countryId, gender);
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            DateOfBirth = dateOfBirth,
            AddressLine = addressLine.Trim(),
            Email = email.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Gender = gender,
            IsActive = true,
            DoNotEmail = doNotEmail,
            DoNotSms = doNotSms,
            CountryId = countryId,
        };

        contact.AddDomainEvent(new ContactCreatedDomainEvent(contact.Id, contact.Email));
        return contact;
    }

    public void UpdateDetails(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string addressLine,
        string email,
        string phoneNumber,
        Guid countryId,
        Gender gender,
        bool doNotEmail,
        bool doNotSms)
    {
        Validate(firstName, lastName, dateOfBirth, addressLine, email, phoneNumber, countryId, gender);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        AddressLine = addressLine.Trim();
        Email = email.Trim();
        PhoneNumber = phoneNumber.Trim();
        CountryId = countryId;
        Gender = gender;
        DoNotEmail = doNotEmail;
        DoNotSms = doNotSms;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetCommunicationPreferences(bool doNotEmail, bool doNotSms)
    {
        DoNotEmail = doNotEmail;
        DoNotSms = doNotSms;
    }

    private static void Validate(
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        string addressLine,
        string email,
        string phoneNumber,
        Guid countryId,
        Gender gender)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ArgumentOutOfRangeException(nameof(dateOfBirth), "Date of birth cannot be in the future.");
        }

        if (countryId == Guid.Empty)
        {
            throw new ArgumentException("Country ID is required.", nameof(countryId));
        }

        if (!Enum.IsDefined(gender))
        {
            throw new ArgumentOutOfRangeException(nameof(gender));
        }
    }
}
