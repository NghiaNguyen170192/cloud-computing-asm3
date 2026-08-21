using NetCore.Donation.Application.Contact.SetPreferences;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Contact.SetPreferences;

[TestClass]
public class SetContactPreferencesCommandHandlerTest : BaseTest
{
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;

    public SetContactPreferencesCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
    }

    [TestMethod]
    public async Task SetContactPreferencesCommand_ShouldUpdateFlagsWhenContactExists()
    {
        var country = Domain.Entities.Country.Create("Australia", "036", "AU", "AUS");
        await countryRepository.AddAsync(country, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var contact = Domain.Entities.Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Analytical Engine Way",
            "ada@example.com",
            "123456",
            country.Id);
        await contactRepository.AddAsync(contact, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var handler = new SetContactPreferencesCommandHandler(unitOfWork, contactRepository);
        var result = await handler.Handle(
            new SetContactPreferencesCommand(contact.Id, true, true),
            default);

        Assert.IsTrue(result);

        var updated = await contactRepository.FindByIdAsync(contact.Id, CancellationToken.None);
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated.DoNotEmail);
        Assert.IsTrue(updated.DoNotSms);
    }

    [TestMethod]
    public async Task SetContactPreferencesCommand_ShouldReturnFalseWhenContactIsMissing()
    {
        var handler = new SetContactPreferencesCommandHandler(unitOfWork, contactRepository);

        var result = await handler.Handle(
            new SetContactPreferencesCommand(Guid.NewGuid(), true, false),
            default);

        Assert.IsFalse(result);
    }
}

