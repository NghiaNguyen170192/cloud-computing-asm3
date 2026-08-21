using NetCore.Donation.Application.Contact.Create;
using NetCore.Donation.Application.Contact.GetContact;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Contact.Create;

[TestClass]
public class CreateContactCommandHandlerTest : BaseTest
{
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateContactCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
    }

    [TestMethod]
    public async Task CreateContactCommand_ShouldReturnValidGuid()
    {
        // Arrange
        var country = Domain.Entities.Country.Create("Australia", "036", "AU", "AUS");
        await countryRepository.AddAsync(country, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var command = new CreateContactCommand(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Analytical Engine Way",
            "ada@example.com",
            "123456",
            country.Id);

        var handler = new CreateContactCommandHandler(unitOfWork, contactRepository, countryRepository);

        // Act
        var id = await handler.Handle(command, default);

        // Assert
        Assert.AreNotEqual(Guid.Empty, id);

        var contact = await new GetContactQueryHandler(contactRepository)
            .Handle(new GetContactQuery(id), default);

        Assert.IsNotNull(contact);
        Assert.AreEqual("Ada", contact.FirstName);
        Assert.AreEqual(country.Id, contact.CountryId);
        Assert.AreEqual("Australia", contact.CountryName);
        Assert.IsTrue(contact.IsActive);
        Assert.IsFalse(contact.DoNotEmail);
        Assert.IsFalse(contact.DoNotSms);
    }

    [TestMethod]
    public async Task CreateContactCommand_ShouldPersistExplicitCommunicationPreferences()
    {
        var country = Domain.Entities.Country.Create("Australia", "036", "AU", "AUS");
        await countryRepository.AddAsync(country, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var command = new CreateContactCommand(
            "Grace",
            "Hopper",
            new DateOnly(1906, 12, 9),
            "1 Compiler Road",
            "grace@example.com",
            "654321",
            country.Id,
            DoNotEmail: true,
            DoNotSms: true);

        var handler = new CreateContactCommandHandler(unitOfWork, contactRepository, countryRepository);

        var id = await handler.Handle(command, default);

        var contact = await new GetContactQueryHandler(contactRepository)
            .Handle(new GetContactQuery(id), default);

        Assert.IsNotNull(contact);
        Assert.IsTrue(contact.DoNotEmail);
        Assert.IsTrue(contact.DoNotSms);
    }

    [TestMethod]
    public async Task CreateContactCommand_ShouldThrowWhenCountryIsUnknown()
    {
        // Arrange
        var command = new CreateContactCommand(
            "Grace",
            "Hopper",
            new DateOnly(1906, 12, 9),
            "1 Compiler Road",
            "grace@example.com",
            "654321",
            Guid.NewGuid());

        var handler = new CreateContactCommandHandler(unitOfWork, contactRepository, countryRepository);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, default));
    }
}