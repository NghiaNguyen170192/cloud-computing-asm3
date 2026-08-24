using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Application.Donation.CompleteDonationTransaction;
using NetCore.Donation.Application.Donation.UserMakesDonation;
using NetCore.Donation.Application.Extensions;
using NetCore.Donation.Application.Outbox.Process;
using NetCore.Donation.Application.PaymentSchedule.Create;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;
using NetCore.Donation.Domain.Messaging;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Domain.Storage;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Infrastructure.Database.Messaging;
using NetCore.Donation.Infrastructure.Storage;

namespace NetCore.Donation.Application.Tests.Donation;

[TestClass]
public class DonationCommandPipelineTest : BaseTest
{
    [TestMethod]
    public async Task UserMakesDonation_WhenOneOff_PersistsContactMethodAndPendingTransactionWithoutSchedule()
    {
        await using var context = await GetContext();
        var countryId = await SeedCountryAsync(context);
        var handler = new UserMakesDonationCommandHandler(
            context,
            new CountryRepository(context),
            new ContactRepository(context),
            new PaymentMethodRepository(context),
            new PaymentScheduleRepository(context),
            new TransactionRepository(context));

        var result = await handler.Handle(CreateCommand(countryId), CancellationToken.None);

        Assert.AreNotEqual(Guid.Empty, result.ContactId);
        Assert.AreNotEqual(Guid.Empty, result.PaymentMethodId);
        Assert.IsNull(result.PaymentScheduleId);
        Assert.IsNotNull(result.TransactionId);
        Assert.IsFalse(result.IsRecurring);
        Assert.AreEqual(0, await context.PaymentSchedules.CountAsync());
        Assert.AreEqual(1, await context.Transactions.CountAsync());
        Assert.AreEqual(TransactionStatus.Pending, (await context.Transactions.SingleAsync()).Status);
        Assert.IsNull((await context.Transactions.SingleAsync()).PaymentScheduleId);
        Assert.AreEqual(0, await context.Journals.CountAsync());
        Assert.AreEqual(0, await context.Receipts.CountAsync());

        var messageTypes = await context.OutboxMessages.Select(message => message.MessageType).ToListAsync();
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(ContactCreatedDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(PaymentMethodCreatedDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(TransactionCreatedDomainEvent))));
        Assert.IsFalse(messageTypes.Any(type => type.Contains(nameof(TransactionPendingDomainEvent))));
        Assert.IsFalse(messageTypes.Any(type => type.Contains(nameof(PaymentScheduleCreatedDomainEvent))));
    }

    [TestMethod]
    public async Task OutboxPipeline_WhenTransactionSucceeds_CreatesReceiptAndJournalInParallel()
    {
        await using var provider = BuildProvider(succeed: true);
        await using var startScope = provider.CreateAsyncScope();
        var startContext = startScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        await startContext.Database.EnsureCreatedAsync();
        var countryId = await SeedCountryAsync(startContext);

        var mediator = startScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(CreateCommand(countryId));
        await DrainOutboxAsync(mediator);

        await using var assertScope = provider.CreateAsyncScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var transaction = await context.Transactions.SingleAsync();
        Assert.IsNull(transaction.PaymentScheduleId);
        Assert.AreEqual(result.TransactionId, transaction.Id);
        Assert.AreEqual(TransactionStatus.Succeeded, transaction.Status);
        Assert.AreEqual(1, await context.Receipts.CountAsync());
        Assert.AreEqual(1, await context.Journals.CountAsync());

        var messageTypes = await context.OutboxMessages.Select(message => message.MessageType).ToListAsync();
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(TransactionCreatedDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(TransactionPendingDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(TransactionCompletedDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(ReceiptCreatedDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(ReceiptGeneratedDomainEvent))));
        Assert.IsTrue(messageTypes.Any(type => type.Contains(nameof(JournalEntryCreatedDomainEvent))));
        Assert.IsTrue(await context.OutboxMessages.AllAsync(message => message.ProcessedAtUtc != null));
    }

    [TestMethod]
    public async Task OutboxPipeline_WhenTransactionFails_DoesNotCreateReceiptOrJournal()
    {
        await using var provider = BuildProvider(succeed: false);
        await using var startScope = provider.CreateAsyncScope();
        var startContext = startScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        await startContext.Database.EnsureCreatedAsync();
        var countryId = await SeedCountryAsync(startContext);

        var mediator = startScope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(CreateCommand(countryId));
        await DrainOutboxAsync(mediator);

        await using var assertScope = provider.CreateAsyncScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var transaction = await context.Transactions.SingleAsync();
        Assert.AreEqual(TransactionStatus.Failed, transaction.Status);
        Assert.AreEqual(0, await context.Receipts.CountAsync());
        Assert.AreEqual(0, await context.Journals.CountAsync());
        Assert.IsTrue(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(TransactionFailedDomainEvent))));
        Assert.IsFalse(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(ReceiptGeneratedDomainEvent))));
        Assert.IsFalse(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(JournalEntryCreatedDomainEvent))));
    }

    [TestMethod]
    public async Task UserMakesDonation_WhenRecurring_StoresIntervalOnSchedule()
    {
        await using var context = await GetContext();
        var countryId = await SeedCountryAsync(context);
        var handler = new UserMakesDonationCommandHandler(
            context,
            new CountryRepository(context),
            new ContactRepository(context),
            new PaymentMethodRepository(context),
            new PaymentScheduleRepository(context),
            new TransactionRepository(context));

        var result = await handler.Handle(
            CreateCommand(countryId, isRecurring: true, RecurringInterval.Monthly),
            CancellationToken.None);

        Assert.IsTrue(result.IsRecurring);
        Assert.IsNotNull(result.PaymentScheduleId);
        Assert.IsNull(result.TransactionId);
        Assert.AreEqual(RecurringInterval.Monthly, (await context.PaymentSchedules.SingleAsync()).RecurringInterval);
        Assert.AreEqual(PaymentType.CreditCard, (await context.PaymentSchedules.SingleAsync()).PaymentType);
        Assert.AreEqual(0, await context.Transactions.CountAsync());
    }

    [TestMethod]
    public async Task CreatePaymentSchedule_CompletesFirstTransactionOnBookDateThroughOutbox()
    {
        await using var provider = BuildProvider(succeed: true);
        await using var startScope = provider.CreateAsyncScope();
        var startContext = startScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        await startContext.Database.EnsureCreatedAsync();
        var countryId = await SeedCountryAsync(startContext);
        var contact = Domain.Entities.Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Analytical Engine Way",
            "ada@example.com",
            "123456",
            countryId);
        startContext.Contacts.Add(contact);
        await startContext.SaveChangesAsync(CancellationToken.None);

        var paymentMethod = Domain.Entities.PaymentMethod.Create(contact.Id, "Visa", PaymentType.Bank);
        startContext.PaymentMethods.Add(paymentMethod);
        await startContext.SaveChangesAsync(CancellationToken.None);

        var bookDate = new DateOnly(2026, 8, 15);
        var mediator = startScope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CreatePaymentScheduleCommand(
            contact.Id,
            paymentMethod.Id,
            50m,
            bookDate,
            RecurringInterval.Monthly,
            PaymentType.Bank));
        await DrainOutboxAsync(mediator);

        await using var assertScope = provider.CreateAsyncScope();
        var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var schedule = await context.PaymentSchedules.SingleAsync();
        var transaction = await context.Transactions.SingleAsync();
        Assert.AreEqual(schedule.Id, transaction.PaymentScheduleId);
        Assert.AreEqual(bookDate, transaction.BookDate);
        Assert.AreEqual(TransactionStatus.Succeeded, transaction.Status);
        Assert.AreEqual(1, await context.Journals.CountAsync());
        Assert.AreEqual(1, await context.Receipts.CountAsync());
        Assert.IsTrue(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(PaymentScheduleCreatedDomainEvent))));
        Assert.IsTrue(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(TransactionCreatedDomainEvent))));
        Assert.IsTrue(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(TransactionPendingDomainEvent))));
        Assert.IsTrue(
            await context.OutboxMessages.AnyAsync(message =>
                message.MessageType.Contains(nameof(TransactionCompletedDomainEvent))));
    }

    private static UserMakesDonationCommand CreateCommand(
        Guid countryId,
        bool isRecurring = false,
        RecurringInterval recurringInterval = RecurringInterval.OneOff)
    {
        return new UserMakesDonationCommand(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Analytical Engine Way",
            "ada@example.com",
            "123456",
            countryId,
            40m,
            "Visa",
            PaymentType.CreditCard,
            isRecurring,
            recurringInterval);
    }

    private static async Task<Guid> SeedCountryAsync(ApplicationDatabaseContext context)
    {
        var country = Domain.Entities.Country.Create("Australia", "036", "AU", "AUS");
        context.Countries.Add(country);
        await context.SaveChangesAsync(CancellationToken.None);
        return country.Id;
    }

    private static async Task DrainOutboxAsync(IMediator mediator)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var processed = await mediator.Send(new ProcessOutboxMessagesCommand());
            if (processed == 0)
            {
                return;
            }
        }

        Assert.Fail("Outbox pipeline did not drain within the expected number of processor cycles.");
    }

    private static ServiceProvider BuildProvider(bool succeed)
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<ApplicationDatabaseContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IDonationTransactionOutcome>(new FixedDonationTransactionOutcome(succeed));
        services.AddScoped(_ => new ApplicationDatabaseContext(options));
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDatabaseContext>());
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IPaymentScheduleRepository, PaymentScheduleRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IJournalRepository, JournalRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddSingleton<RecordingIntegrationEventPublisher>();
        services.AddSingleton<IIntegrationEventPublisher>(
            provider => provider.GetRequiredService<RecordingIntegrationEventPublisher>());
        services.AddSingleton<IReceiptDocumentStorage, InMemoryReceiptDocumentStorage>();
        services.AddSingleton<IReceiptDocumentGenerator, ReceiptPdfDocumentGenerator>();
        return services.BuildServiceProvider();
    }

    private sealed class FixedDonationTransactionOutcome(bool succeed) : IDonationTransactionOutcome
    {
        public bool IsSuccess() => succeed;
    }
}
