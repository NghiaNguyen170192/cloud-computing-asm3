using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Contact.Create;

public class CreateContactCommandHandler(
    IUnitOfWork unitOfWork,
    IContactRepository contactRepository,
    ICountryRepository countryRepository)
    : IRequestHandler<CreateContactCommand, Guid>
{
    public async Task<Guid> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        var country = await countryRepository.FindByIdAsync(request.CountryId);
        if (country is null)
        {
            throw new ArgumentException($"Country '{request.CountryId}' was not found.", nameof(request));
        }

        var contact = request.ToDbEntity();

        await contactRepository.AddAsync(contact, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return contact.Id;
    }
}