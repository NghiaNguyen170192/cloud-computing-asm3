using MediatR;
using NetCore.Donation.Application.Contact.DTOs;

namespace NetCore.Donation.Application.Contact.QueryContacts;

public sealed record QueryContacts : IRequest<IQueryable<QueryContactDto>>;