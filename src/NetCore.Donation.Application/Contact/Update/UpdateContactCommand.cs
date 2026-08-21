using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.Contact.Update;

public sealed record UpdateContactCommand(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string AddressLine,
    string Email,
    string PhoneNumber,
    Guid CountryId,
    bool DoNotEmail,
    bool DoNotSms,
    Gender Gender = Gender.Other) : IRequest<bool>;
