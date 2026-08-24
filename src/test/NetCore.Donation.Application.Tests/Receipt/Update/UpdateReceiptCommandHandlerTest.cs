using NetCore.Donation.Application.Receipt.Create;
using NetCore.Donation.Application.Receipt.GetReceipt;
using NetCore.Donation.Application.Receipt.Update;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;
using NetCore.Donation.Infrastructure.Storage;

namespace NetCore.Donation.Application.Tests.Receipt.Update;

[TestClass]
public class UpdateReceiptCommandHandlerTest : BaseTest
{
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IPaymentMethodRepository paymentMethodRepository;
    private readonly IPaymentScheduleRepository paymentScheduleRepository;
    private readonly IReceiptRepository receiptRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly ReceiptPdfDocumentGenerator documentGenerator = new();
    private readonly InMemoryReceiptDocumentStorage documentStorage = new();

    public UpdateReceiptCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
        paymentMethodRepository = new PaymentMethodRepository(context);
        paymentScheduleRepository = new PaymentScheduleRepository(context);
        receiptRepository = new ReceiptRepository(context);
        transactionRepository = new TransactionRepository(context);
    }

    [TestMethod]
    public async Task UpdateReceiptCommand_ShouldRegenerateDocumentWhenTransactionLinkageChanges()
    {
        var contact = await SeedContactAsync();
        var transaction = await SeedTransactionAsync(contact.Id);

        var createHandler = new CreateReceiptCommandHandler(
            unitOfWork,
            receiptRepository,
            contactRepository,
            transactionRepository,
            paymentMethodRepository,
            documentGenerator,
            documentStorage);

        var receiptId = await createHandler.Handle(new CreateReceiptCommand(contact.Id), default);
        var beforeUpdate = await new GetReceiptQueryHandler(receiptRepository)
            .Handle(new GetReceiptQuery(receiptId), default);
        Assert.IsNotNull(beforeUpdate);
        var objectKey = $"receipts/{beforeUpdate.Identifier}.pdf";
        Assert.IsTrue(await documentStorage.ExistsAsync(objectKey));
        Assert.IsNull(beforeUpdate.TransactionId);
        Assert.IsTrue(beforeUpdate.HasDocument);
        var generatedAtBefore = beforeUpdate.DocumentGeneratedAtUtc;

        var updateHandler = new UpdateReceiptCommandHandler(
            unitOfWork,
            receiptRepository,
            contactRepository,
            transactionRepository,
            paymentMethodRepository,
            documentGenerator,
            documentStorage);

        var updated = await updateHandler.Handle(
            new UpdateReceiptCommand(receiptId, transaction.Id),
            default);

        Assert.IsTrue(updated);
        Assert.IsTrue(await documentStorage.ExistsAsync(objectKey));

        var afterUpdate = await new GetReceiptQueryHandler(receiptRepository)
            .Handle(new GetReceiptQuery(receiptId), default);
        Assert.IsNotNull(afterUpdate);
        Assert.AreEqual(transaction.Id, afterUpdate.TransactionId);
        Assert.IsTrue(afterUpdate.HasDocument);
        Assert.AreEqual("application/pdf", afterUpdate.DocumentContentType);
        Assert.AreEqual($"{afterUpdate.Identifier}.pdf", afterUpdate.DocumentFileName);
        Assert.IsTrue(afterUpdate.DocumentGeneratedAtUtc >= generatedAtBefore);
    }

    private async Task<Domain.Entities.Contact> SeedContactAsync()
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
        return contact;
    }

    private async Task<Domain.Entities.Transaction> SeedTransactionAsync(Guid contactId)
    {
        var paymentMethod = Domain.Entities.PaymentMethod.Create(contactId, "Visa ending 1234");
        await paymentMethodRepository.AddAsync(paymentMethod, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var bookDate = new DateOnly(2026, 8, 15);
        var schedule = Domain.Entities.PaymentSchedule.Create(
            contactId,
            paymentMethod.Id,
            50m,
            bookDate,
            RecurringInterval.Monthly);
        await paymentScheduleRepository.AddAsync(schedule, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);

        var transaction = Domain.Entities.Transaction.Create(
            50m,
            schedule.Id,
            contactId,
            paymentMethod.Id,
            PaymentType.Bank,
            bookDate,
            bookDate);
        await transactionRepository.AddAsync(transaction, CancellationToken.None);
        await unitOfWork.SaveChangesAsync(default);
        return transaction;
    }
}
