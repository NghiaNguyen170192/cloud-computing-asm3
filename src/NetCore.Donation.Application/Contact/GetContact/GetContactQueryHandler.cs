using MediatR;
using NetCore.Donation.Application.Contact.DTOs;
using NetCore.Donation.Domain.IRepositories;

namespace NetCore.Donation.Application.Contact.GetContact;

public class GetContactQueryHandler(IContactRepository contactRepository)
    : IRequestHandler<GetContactQuery, QueryContactDto?>
{
    public Task<QueryContactDto?> Handle(GetContactQuery request, CancellationToken cancellationToken)
    {
        var contact = contactRepository.GetAll()
            .ToQueryDto()
            .FirstOrDefault(contact => contact.Id == request.Id);

        return Task.FromResult(contact);
    }
}