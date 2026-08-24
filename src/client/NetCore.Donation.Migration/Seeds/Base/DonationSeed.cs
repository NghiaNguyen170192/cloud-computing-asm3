using Bogus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCore.Donation.Application.Donation.QueryDonationFlows;
using NetCore.Donation.Application.Donation.UserMakesDonation;
using NetCore.Donation.Application.Outbox.Process;
using NetCore.Donation.Application.PaymentMethod.Create;
using NetCore.Donation.Application.PaymentSchedule.Create;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.IRepositories;
using NetCore.Donation.Infrastructure.Database;
using NetCore.Donation.Migration.Common.Interface;

namespace NetCore.Donation.Migration.Seeds.Base;

public sealed class DonationSeed(
    ISender dispatcher,
    ApplicationDatabaseContext context,
    ICountryRepository countryRepository,
    ILogger<DonationSeed> logger) : IDataSeed
{
    private const int DefaultRecordCount = 1000;
    private const int Seed = 20260817;

    public IEnumerable<Type> Dependencies => [typeof(CountrySeed)];

    private static int ResolveRecordCount()
    {
        var raw = Environment.GetEnvironmentVariable("SEED_DONATION_COUNT");
        if (int.TryParse(raw, out var count) && count > 0)
        {
            return count;
        }

        return DefaultRecordCount;
    }

    public async Task SeedAsync()
    {
        var recordCount = ResolveRecordCount();
        var existing = await context.Contacts.CountAsync();
        if (existing >= recordCount)
        {
            logger.LogInformation("Skipping donation seed; {Count} contacts already exist.", existing);
            return;
        }

        var countryIds = await countryRepository.GetAll()
            .Select(country => country.Id)
            .ToListAsync();
        if (countryIds.Count == 0)
        {
            throw new InvalidOperationException("Countries must be seeded before donation data.");
        }

        var remaining = recordCount - existing;
        logger.LogInformation(
            "Seeding {Remaining} contacts with one-off gifts and/or payment schedules through the application layer ({Existing} contacts already present; target {Target}).",
            remaining,
            existing,
            recordCount);

        var faker = new Faker("en_AU") { Random = new Randomizer(Seed + existing) };
        var recurringIntervals = Enum.GetValues<RecurringInterval>()
            .Where(interval => interval != RecurringInterval.OneOff)
            .ToArray();
        var paymentTypes = Enum.GetValues<PaymentType>();
        var genders = Enum.GetValues<Gender>();
        var brands = new[] { "Visa", "Mastercard", "Bank account", "PayPal" };
        var now = DateTime.UtcNow;
        var earliest = now.AddYears(-5);

        for (var index = 0; index < remaining; index++)
        {
            var donor = CreateDonor(faker, countryIds, genders, existing + index + 1);
            var pattern = faker.Random.WeightedRandom(
                [GivingPattern.OneOffs, GivingPattern.Schedules, GivingPattern.Mixed],
                [0.45f, 0.35f, 0.20f]);
            var oneOffCount = pattern == GivingPattern.Schedules
                ? 0
                : faker.Random.WeightedRandom([1, 2, 3, 4], [0.50f, 0.25f, 0.15f, 0.10f]);
            var scheduleCount = pattern == GivingPattern.OneOffs
                ? 0
                : faker.Random.WeightedRandom([1, 2, 3], [0.60f, 0.30f, 0.10f]);

            var firstIsRecurring = oneOffCount == 0;
            var firstOccurredAt = UtcBetween(faker, earliest, now);
            var first = await GiveThroughApplicationAsync(
                donor,
                faker,
                brands,
                paymentTypes,
                recurringIntervals,
                firstOccurredAt,
                isRecurring: firstIsRecurring,
                stampContact: true,
                stampPaymentMethod: true);

            var remainingOneOffs = firstIsRecurring ? oneOffCount : oneOffCount - 1;
            for (var gift = 0; gift < remainingOneOffs; gift++)
            {
                await GiveThroughApplicationAsync(
                    donor,
                    faker,
                    brands,
                    paymentTypes,
                    recurringIntervals,
                    UtcBetween(faker, firstOccurredAt, now),
                    isRecurring: false,
                    stampContact: false,
                    stampPaymentMethod: true);
            }

            var remainingSchedules = firstIsRecurring ? scheduleCount - 1 : scheduleCount;
            var methodId = first.PaymentMethodId;
            var methodType = await PaymentTypeOfAsync(methodId);
            for (var gift = 0; gift < remainingSchedules; gift++)
            {
                var useNewMethod = faker.Random.Bool(0.45f);
                var scheduleType = methodType;
                var scheduleMethodId = methodId;
                if (useNewMethod)
                {
                    scheduleType = faker.PickRandom(paymentTypes);
                    scheduleMethodId = await dispatcher.Send(new CreatePaymentMethodCommand(
                        first.ContactId,
                        $"{faker.PickRandom(brands)} {faker.Random.Replace("****")}",
                        scheduleType));
                    context.ChangeTracker.Clear();
                    await DrainOutboxAsync();
                }

                await CreateScheduleDonationAsync(
                    first.ContactId,
                    scheduleMethodId,
                    scheduleType,
                    faker,
                    recurringIntervals,
                    UtcBetween(faker, firstOccurredAt, now),
                    stampPaymentMethod: useNewMethod);
            }

            if ((index + 1) % 10 == 0)
            {
                logger.LogInformation("Seeded {Inserted}/{Total} contacts.", index + 1, remaining);
            }
        }

        await DrainOutboxAsync();
        logger.LogInformation(
            "Finished donation seed for {Count} contacts, including extra gifts and outbox money-flow events.",
            remaining);
    }

    private async Task<UserMakesDonationResult> GiveThroughApplicationAsync(
        DonorProfile donor,
        Faker faker,
        string[] brands,
        PaymentType[] paymentTypes,
        RecurringInterval[] recurringIntervals,
        DateTime occurredAtUtc,
        bool isRecurring,
        bool stampContact,
        bool stampPaymentMethod)
    {
        var paymentType = faker.PickRandom(paymentTypes);
        var batchStarted = DateTime.UtcNow.AddSeconds(-2);
        var result = await dispatcher.Send(new UserMakesDonationCommand(
            donor.FirstName,
            donor.LastName,
            donor.DateOfBirth,
            donor.AddressLine,
            donor.Email,
            donor.PhoneNumber,
            donor.CountryId,
            RandomAmount(faker),
            $"{faker.PickRandom(brands)} {faker.Random.Replace("****")}",
            paymentType,
            isRecurring,
            isRecurring ? faker.PickRandom(recurringIntervals) : RecurringInterval.OneOff,
            donor.Gender,
            donor.DoNotEmail,
            donor.DoNotSms,
            DateOnly.FromDateTime(occurredAtUtc)));

        context.ChangeTracker.Clear();
        await DrainOutboxAsync();
        await BackdateGiftAsync(
            result,
            occurredAtUtc,
            ReceivedDate(faker, occurredAtUtc),
            batchStarted,
            stampContact,
            stampPaymentMethod);
        context.ChangeTracker.Clear();
        return result;
    }

    private async Task CreateScheduleDonationAsync(
        Guid contactId,
        Guid paymentMethodId,
        PaymentType paymentType,
        Faker faker,
        RecurringInterval[] recurringIntervals,
        DateTime occurredAtUtc,
        bool stampPaymentMethod)
    {
        var batchStarted = DateTime.UtcNow.AddSeconds(-2);
        var scheduleId = await dispatcher.Send(new CreatePaymentScheduleCommand(
            contactId,
            paymentMethodId,
            RandomAmount(faker),
            DateOnly.FromDateTime(occurredAtUtc),
            faker.PickRandom(recurringIntervals),
            paymentType));

        context.ChangeTracker.Clear();
        await DrainOutboxAsync();
        await BackdateGiftAsync(
            new UserMakesDonationResult(contactId, paymentMethodId, scheduleId, null, true),
            occurredAtUtc,
            ReceivedDate(faker, occurredAtUtc),
            batchStarted,
            stampContact: false,
            stampPaymentMethod);
        context.ChangeTracker.Clear();
    }

    private async Task<PaymentType> PaymentTypeOfAsync(Guid paymentMethodId) =>
        await context.PaymentMethods
            .AsNoTracking()
            .Where(method => method.Id == paymentMethodId)
            .Select(method => method.PaymentType)
            .SingleAsync();

    private async Task BackdateGiftAsync(
        UserMakesDonationResult result,
        DateTime occurredAtUtc,
        DateOnly receivedDate,
        DateTime batchStartedUtc,
        bool stampContact,
        bool stampPaymentMethod)
    {
        if (stampContact)
        {
            await context.Contacts
                .Where(contact => contact.Id == result.ContactId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(contact => contact.CreatedDate, occurredAtUtc)
                    .SetProperty(contact => contact.ModifiedDate, occurredAtUtc));
        }

        if (stampPaymentMethod)
        {
            await context.PaymentMethods
                .Where(method => method.Id == result.PaymentMethodId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(method => method.CreatedDate, occurredAtUtc)
                    .SetProperty(method => method.ModifiedDate, occurredAtUtc));
        }

        if (result.PaymentScheduleId is { } scheduleId)
        {
            await context.PaymentSchedules
                .Where(schedule => schedule.Id == scheduleId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(schedule => schedule.CreatedDate, occurredAtUtc)
                    .SetProperty(schedule => schedule.ModifiedDate, occurredAtUtc)
                    .SetProperty(schedule => schedule.BookDate, DateOnly.FromDateTime(occurredAtUtc)));
        }

        var transactionId = result.TransactionId
            ?? await context.Transactions
                .Where(transaction => transaction.PaymentScheduleId == result.PaymentScheduleId)
                .Select(transaction => (Guid?)transaction.Id)
                .FirstOrDefaultAsync();

        if (transactionId is { } txnId && txnId != Guid.Empty)
        {
            await context.Transactions
                .Where(transaction => transaction.Id == txnId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(transaction => transaction.CreatedDate, occurredAtUtc)
                    .SetProperty(transaction => transaction.ModifiedDate, occurredAtUtc)
                    .SetProperty(transaction => transaction.BookDate, DateOnly.FromDateTime(occurredAtUtc))
                    .SetProperty(transaction => transaction.ReceivedDate, receivedDate));

            await context.Journals
                .Where(journal => journal.TransactionId == txnId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(journal => journal.CreatedDate, occurredAtUtc)
                    .SetProperty(journal => journal.ModifiedDate, occurredAtUtc));

            await context.Receipts
                .Where(receipt => receipt.TransactionId == txnId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(receipt => receipt.CreatedDate, occurredAtUtc)
                    .SetProperty(receipt => receipt.ModifiedDate, occurredAtUtc)
                    .SetProperty(receipt => receipt.DocumentGeneratedAtUtc, occurredAtUtc));
        }

        var batch = await context.OutboxMessages
            .Where(message => message.OccurredAtUtc >= batchStartedUtc)
            .Select(message => new { message.Id, message.MessageType, message.OccurredAtUtc })
            .ToListAsync();

        var ordered = batch
            .OrderBy(message => DonationFlowAssembler.CanonicalSequence(DonationFlowAssembler.ToEventName(message.MessageType)))
            .ThenBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .ToList();

        for (var offset = 0; offset < ordered.Count; offset++)
        {
            var stamp = occurredAtUtc.AddSeconds(offset);
            var messageId = ordered[offset].Id;
            await context.OutboxMessages
                .Where(message => message.Id == messageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(message => message.OccurredAtUtc, stamp)
                    .SetProperty(message => message.ProcessedAtUtc, stamp));
        }
    }

    private async Task DrainOutboxAsync()
    {
        for (var attempt = 0; attempt < DefaultRecordCount; attempt++)
        {
            var processed = await dispatcher.Send(new ProcessOutboxMessagesCommand(100));
            context.ChangeTracker.Clear();
            if (processed == 0)
            {
                return;
            }
        }

        logger.LogWarning("Stopped draining the outbox after the attempt cap; some money-flow events may still be pending.");
    }

    private static DonorProfile CreateDonor(
        Faker faker,
        IList<Guid> countryIds,
        Gender[] genders,
        int index)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        return new DonorProfile(
            firstName,
            lastName,
            DateOnly.FromDateTime(faker.Date.Past(50, DateTime.UtcNow.AddYears(-21))),
            faker.Address.StreetAddress(),
            EmailFor(firstName, lastName, index),
            faker.Random.Replace("+614########"),
            faker.PickRandom(countryIds),
            faker.PickRandom(genders),
            faker.Random.Bool(0.2f),
            faker.Random.Bool(0.2f));
    }

    private static decimal RandomAmount(Faker faker) => decimal.Round(faker.Finance.Amount(5, 2500), 2);

    private static DateTime UtcBetween(Faker faker, DateTime start, DateTime end) =>
        DateTime.SpecifyKind(faker.Date.Between(start, end), DateTimeKind.Utc);

    private static DateOnly ReceivedDate(Faker faker, DateTime occurredAtUtc)
    {
        var bookDate = DateOnly.FromDateTime(occurredAtUtc);
        var receivedDate = bookDate.AddDays(faker.Random.Int(0, 14));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return receivedDate > today ? today : receivedDate;
    }

    private static string EmailFor(string firstName, string lastName, int index)
    {
        var token = $"{firstName}.{lastName}.{index}"
            .ToLowerInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal);
        return $"{token}@donors.test";
    }

    private enum GivingPattern
    {
        OneOffs,
        Schedules,
        Mixed,
    }

    private sealed record DonorProfile(
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string AddressLine,
        string Email,
        string PhoneNumber,
        Guid CountryId,
        Gender Gender,
        bool DoNotEmail,
        bool DoNotSms);
}

