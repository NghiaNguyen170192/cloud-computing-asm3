using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Country.Delete;

public class DeleteCountriesCommandHandler(
    IUnitOfWork unitOfWork,
    ICountryRepository countryRepository)
    : IRequestHandler<DeleteCountriesCommand, bool>,
      IRequestHandler<DeleteCountryCommand, bool>
{
    public async Task<bool> Handle(DeleteCountriesCommand request, CancellationToken cancellationToken)
    {
        var countries = new List<Domain.Entities.Country>();

        foreach (var id in request.Ids)
        {
            var country = await countryRepository.FindByIdAsync(id);
            if (country is not null)
            {
                countries.Add(country);
            }
        }

        countryRepository.Delete(countries);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> Handle(DeleteCountryCommand request, CancellationToken cancellationToken)
    {
        var country = await countryRepository.FindByIdAsync(request.Id);
        if (country is null)
        {
            return false;
        }

        countryRepository.Delete(country);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}