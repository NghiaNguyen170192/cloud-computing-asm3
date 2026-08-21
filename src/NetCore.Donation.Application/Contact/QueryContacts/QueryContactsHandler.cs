using MediatR;
using NetCore.Donation.Application.Contact.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Contact.QueryContacts;

public class QueryContactsHandler(IContactRepository contactRepository)
    : IRequestHandler<QueryContacts, IQueryable<QueryContactDto>>
{
    public Task<IQueryable<QueryContactDto>> Handle(QueryContacts request, CancellationToken cancellationToken)
    {
        return Task.FromResult(contactRepository.GetAll().ToQueryDto());
    }
}