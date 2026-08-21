using NetCore.Donation.Application.Journal.Create;
using NetCore.Donation.Application.Journal.Delete;
using NetCore.Donation.Application.Journal.GetJournal;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;

namespace NetCore.Donation.Application.Tests.Journal.Create;

[TestClass]
public class CreateJournalCommandHandlerTest : BaseTest
{
    private readonly IJournalRepository journalRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IPaymentMethodRepository paymentMethodRepository;
    private readonly IPaymentScheduleRepository paymentScheduleRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateJournalCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        journalRepository = new JournalRepository(context);
        transactionRepository = new TransactionRepository(context);
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
        paymentMethodRepository = new PaymentMethodRepository(context);
        paymentScheduleRepository = new PaymentScheduleRepository(context);
    }

    [TestMethod]
    public async Task CreateJournalCommand_ShouldReturnValidGuid()
    {
        var transactionId = await CreateTransactionAsync();
        var handler = new CreateJournalCommandHandler(unitOfWork, journalRepository, transactionRepository);

        var id = await handler.Handle(new CreateJournalCommand(transactionId), default);

        Assert.AreNotEqual(Guid.Empty, id);

        var journal = await new GetJournalQueryHandler(journalRepository)
            .Handle(new GetJournalQuery(id), default);

        Assert.IsNotNull(journal);
        Assert.AreEqual(id, journal.Id);
    }

    [TestMethod]
    public async Task DeleteJournalCommand_ShouldReturnFalseWhenMissing()
    {
        var handler = new DeleteJournalCommandHandler(unitOfWork, journalRepository);

        var deleted = await handler.Handle(new DeleteJournalCommand(Guid.NewGuid()), default);

        Assert.IsFalse(deleted);
    }

    private async Task<Guid> CreateTransactionAsync()
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

        var paymentMethod = Domain.Entities.PaymentMethod.Create(contact.Id, "Visa");
        await paymentMethodRepository.AddAsync(paymentMethod, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var schedule = Domain.Entities.PaymentSchedule.Create(
            contact.Id,
            paymentMethod.Id,
            25m,
            new DateOnly(2026, 8, 15),
            RecurringInterval.Monthly);
        await paymentScheduleRepository.AddAsync(schedule, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var transaction = Domain.Entities.Transaction.Create(
            25m,
            schedule.Id,
            contact.Id,
            paymentMethod.Id,
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 8, 15));
        await transactionRepository.AddAsync(transaction, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        return transaction.Id;
    }
}
