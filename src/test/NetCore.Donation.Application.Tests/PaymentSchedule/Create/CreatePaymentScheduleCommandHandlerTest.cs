using NetCore.Donation.Application.PaymentSchedule.Create;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.PaymentSchedule.Create;

[TestClass]
public class CreatePaymentScheduleCommandHandlerTest : BaseTest
{
    private readonly ApplicationDatabaseContext context;
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IPaymentMethodRepository paymentMethodRepository;
    private readonly IPaymentScheduleRepository paymentScheduleRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreatePaymentScheduleCommandHandlerTest()
    {
        context = GetContext().Result;
        unitOfWork = context;
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
        paymentMethodRepository = new PaymentMethodRepository(context);
        paymentScheduleRepository = new PaymentScheduleRepository(context);
    }

    [TestMethod]
    public async Task CreatePaymentScheduleCommand_ShouldReturnValidGuid()
    {
        // Arrange
        var owner = await CreateContact("Owner", "One", "one@example.com", "111");
        var paymentMethod = Domain.Entities.PaymentMethod.Create(owner.Id, "Visa ending 1234");
        await paymentMethodRepository.AddAsync(paymentMethod, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var command = new CreatePaymentScheduleCommand(
            owner.Id,
            paymentMethod.Id,
            25m,
            new DateOnly(2026, 8, 15),
            RecurringInterval.Monthly);

        var handler = new CreatePaymentScheduleCommandHandler(
            unitOfWork,
            paymentScheduleRepository,
            contactRepository,
            paymentMethodRepository);

        // Act
        var id = await handler.Handle(command, default);

        // Assert
        Assert.AreNotEqual(Guid.Empty, id);
        Assert.IsTrue(
            context.OutboxMessages.Any(message => message.MessageType.Contains(nameof(PaymentScheduleCreatedDomainEvent))));
        Assert.AreEqual(0, context.Transactions.Count());
    }

    [TestMethod]
    public async Task CreatePaymentScheduleCommand_ShouldRejectPaymentMethodOwnedByAnotherContact()
    {
        // Arrange
        var owner = await CreateContact("Owner", "One", "one@example.com", "111");
        var other = await CreateContact("Owner", "Two", "two@example.com", "222");
        var paymentMethod = Domain.Entities.PaymentMethod.Create(owner.Id, "Visa ending 1234");
        await paymentMethodRepository.AddAsync(paymentMethod, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var command = new CreatePaymentScheduleCommand(
            other.Id,
            paymentMethod.Id,
            25m,
            new DateOnly(2026, 8, 15),
            RecurringInterval.Monthly);

        var handler = new CreatePaymentScheduleCommandHandler(
            unitOfWork,
            paymentScheduleRepository,
            contactRepository,
            paymentMethodRepository);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, default));
    }

    private async Task<Domain.Entities.Contact> CreateContact(
        string firstName,
        string lastName,
        string email,
        string phoneNumber)
    {
        var country = countryRepository.GetAll().FirstOrDefault();
        if (country is null)
        {
            country = Domain.Entities.Country.Create("Australia", "036", "AU", "AUS");
            await countryRepository.AddAsync(country, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(default);
        }

        var contact = Domain.Entities.Contact.Create(
            firstName,
            lastName,
            new DateOnly(1990, 1, 1),
            "1 Donation Street",
            email,
            phoneNumber,
            country.Id);

        await contactRepository.AddAsync(contact, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        return contact;
    }
}