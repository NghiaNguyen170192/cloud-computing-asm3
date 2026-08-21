using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Application.Country.Create;

public class CreateCountriesCommandHandler(
    IUnitOfWork unitOfWork,
    ICountryRepository countryRepository)
    : MediatR.IRequestHandler<CreateCountriesCommand, IEnumerable<Guid>>,
      MediatR.IRequestHandler<CreateCountryCommand, Guid>
{
    // MediatR handler methods
    public async Task<IEnumerable<Guid>> Handle(CreateCountriesCommand request, CancellationToken cancellationToken)
    {
        var countries = request.Countries.Select(ToDbEntity).ToList();

        await countryRepository.AddAsync(countries, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        //TODO cache doesnt work
        //await cacheRepository.AddAsync(countries);

        return countries.Select(x => x.Id);
    }

    public async Task<Guid> Handle(CreateCountryCommand request, CancellationToken cancellationToken)
    {
        var country = request.ToDbEntity();
        await countryRepository.AddAsync(country, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        //TODO: Add to cache not using domain entity, but using a DTO or a cache model to avoid caching domain entities directly.
        //await cacheRepository.AddAsync(country);

        return country.Id;
    }

    private Domain.Entities.Country ToDbEntity(CreateCountryCommand request)
    {
        return Domain.Entities.Country.Create(request.Name, request.CountryCode, request.Alpha2, request.Alpha3);
    }
}
