using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Contact.Update;

public class UpdateContactCommandHandler(
    IUnitOfWork unitOfWork,
    IContactRepository contactRepository,
    ICountryRepository countryRepository)
    : IRequestHandler<UpdateContactCommand, bool>
{
    public async Task<bool> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await contactRepository.FindByIdAsync(request.Id, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        var country = await countryRepository.FindByIdAsync(request.CountryId);
        if (country is null)
        {
            throw new ArgumentException($"Country '{request.CountryId}' was not found.", nameof(request));
        }

        request.UpdateEntity(contact);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}