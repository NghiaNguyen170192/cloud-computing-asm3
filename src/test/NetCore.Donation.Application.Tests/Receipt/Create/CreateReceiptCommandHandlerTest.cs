using NetCore.Donation.Application.Receipt.Create;
using NetCore.Donation.Application.Receipt.Delete;
using NetCore.Donation.Application.Receipt.GetReceipt;
using NetCore.Donation.Application.Receipt.GetReceiptDocument;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Repositories;
using NetCore.Donation.Infrastructure.Storage;

namespace NetCore.Donation.Application.Tests.Receipt.Create;

[TestClass]
public class CreateReceiptCommandHandlerTest : BaseTest
{
    private readonly IContactRepository contactRepository;
    private readonly ICountryRepository countryRepository;
    private readonly IReceiptRepository receiptRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly BlankReceiptDocumentGenerator documentGenerator = new();
    private readonly InMemoryReceiptDocumentStorage documentStorage = new();

    public CreateReceiptCommandHandlerTest()
    {
        var context = GetContext().Result;
        unitOfWork = context;
        contactRepository = new ContactRepository(context);
        countryRepository = new CountryRepository(context);
        receiptRepository = new ReceiptRepository(context);
        transactionRepository = new TransactionRepository(context);
    }

    [TestMethod]
    public async Task CreateReceiptCommand_ShouldUploadDocumentAndPersistMetadata()
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

        var handler = new CreateReceiptCommandHandler(
            unitOfWork,
            receiptRepository,
            contactRepository,
            transactionRepository,
            documentGenerator,
            documentStorage);

        var id = await handler.Handle(new CreateReceiptCommand(contact.Id), default);

        var metadata = await new GetReceiptQueryHandler(receiptRepository)
            .Handle(new GetReceiptQuery(id), default);
        Assert.IsNotNull(metadata);
        Assert.IsTrue(metadata.HasDocument);
        Assert.AreEqual("application/pdf", metadata.DocumentContentType);

        var document = await new GetReceiptDocumentQueryHandler(receiptRepository, documentStorage)
            .Handle(new GetReceiptDocumentQuery(id), default);
        Assert.IsNotNull(document);
        Assert.AreEqual("application/pdf", document.ContentType);
        Assert.IsTrue(document.SizeBytes > 0);
        await document.Content.DisposeAsync();
    }

    [TestMethod]
    public async Task DeleteReceiptCommand_ShouldRemoveStoredDocument()
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

        var createHandler = new CreateReceiptCommandHandler(
            unitOfWork,
            receiptRepository,
            contactRepository,
            transactionRepository,
            documentGenerator,
            documentStorage);
        var id = await createHandler.Handle(new CreateReceiptCommand(contact.Id), default);
        var objectKey = $"receipts/{id:N}.pdf";
        Assert.IsTrue(await documentStorage.ExistsAsync(objectKey));

        var deleteHandler = new DeleteReceiptCommandHandler(unitOfWork, receiptRepository, documentStorage);
        var deleted = await deleteHandler.Handle(new DeleteReceiptCommand(id), default);

        Assert.IsTrue(deleted);
        Assert.IsFalse(await documentStorage.ExistsAsync(objectKey));
    }
}
