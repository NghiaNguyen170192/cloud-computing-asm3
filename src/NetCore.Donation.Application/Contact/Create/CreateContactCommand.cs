using MediatR;
using NetCore.Donation.Domain.Enums;

namespace NetCore.Donation.Application.Contact.Create;

public sealed record CreateContactCommand(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string AddressLine,
    string Email,
    string PhoneNumber,
    Guid CountryId,
    Gender Gender = Gender.Other,
    bool DoNotEmail = false,
    bool DoNotSms = false) : IRequest<Guid>;
