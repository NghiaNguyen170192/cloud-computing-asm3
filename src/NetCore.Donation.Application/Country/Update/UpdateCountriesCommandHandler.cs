using MediatR;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Country.Update;

public class UpdateCountriesCommandHandler(
    IUnitOfWork unitOfWork,
    ICountryRepository countryRepository)
    : IRequestHandler<UpdateCountriesCommand, bool>,
      IRequestHandler<UpdateCountryCommand, bool>
{
    public async Task<bool> Handle(UpdateCountriesCommand request, CancellationToken cancellationToken)
    {
        foreach (var countryCommand in request.Countries)
        {
            var country = await countryRepository.FindByIdAsync(countryCommand.Id);
            if (country is not null)
            {
                countryCommand.UpdateEntity(country);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        var country = await countryRepository.FindByIdAsync(request.Id);
        if (country is null)
        {
            return false;
        }

        request.UpdateEntity(country);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}