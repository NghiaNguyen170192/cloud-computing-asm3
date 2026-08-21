using MediatR;
using NetCore.Donation.Application.Contact.DTOs;

namespace NetCore.Donation.Application.Contact.GetContact;

public sealed record GetContactQuery(Guid Id) : IRequest<QueryContactDto?>;