using NetCore.Donation.Application.Contact.Update;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Contact.Update;

[TestClass]
public class UpdateContactCommandHandlerTest : BaseTest
{
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;

    public UpdateContactCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
    }

    [TestMethod]
    public async Task UpdateContactCommand_ShouldReturnTrueWhenContactExists()
    {
        // Arrange
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

        var command = new UpdateContactCommand(
            contact.Id,
            "Augusta",
            "King",
            new DateOnly(1815, 12, 10),
            "2 Analytical Engine Way",
            "augusta@example.com",
            "999888",
            country.Id,
            true,
            false);

        var handler = new UpdateContactCommandHandler(unitOfWork, contactRepository, countryRepository);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        Assert.IsTrue(result);

        var updated = await contactRepository.FindByIdAsync(contact.Id, CancellationToken.None);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Augusta", updated.FirstName);
        Assert.AreEqual("augusta@example.com", updated.Email);
        Assert.IsTrue(updated.DoNotEmail);
        Assert.IsFalse(updated.DoNotSms);
    }

    [TestMethod]
    public async Task UpdateContactCommand_ShouldReturnFalseWhenContactIsMissing()
    {
        // Arrange
        var command = new UpdateContactCommand(
            Guid.NewGuid(),
            "Augusta",
            "King",
            new DateOnly(1815, 12, 10),
            "2 Analytical Engine Way",
            "augusta@example.com",
            "999888",
            Guid.NewGuid(),
            false,
            true);

        var handler = new UpdateContactCommandHandler(unitOfWork, contactRepository, countryRepository);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        Assert.IsFalse(result);
    }
}