using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Domain.Events;

namespace NetCore.Donation.Domain.Tests.Entities;

[TestClass]
public class DonationEntityTests
{
    [TestMethod]
    public void ContactCreate_WithValidDetails_CreatesActiveContact()
    {
        var countryId = Guid.NewGuid();

        var contact = Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Computing Lane",
            "ada@example.com",
            "+61 400 000 000",
            countryId);

        Assert.AreEqual("Ada", contact.FirstName);
        Assert.AreEqual(countryId, contact.CountryId);
        Assert.IsTrue(contact.IsActive);
        Assert.IsFalse(contact.DoNotEmail);
        Assert.IsFalse(contact.DoNotSms);
    }

    [TestMethod]
    public void ContactCreate_WithCommunicationPreferences_PersistsFlags()
    {
        var contact = Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Computing Lane",
            "ada@example.com",
            "+61 400 000 000",
            Guid.NewGuid(),
            doNotEmail: true,
            doNotSms: true);

        Assert.IsTrue(contact.DoNotEmail);
        Assert.IsTrue(contact.DoNotSms);
    }

    [TestMethod]
    public void ContactSetCommunicationPreferences_UpdatesFlags()
    {
        var contact = Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Computing Lane",
            "ada@example.com",
            "+61 400 000 000",
            Guid.NewGuid());

        contact.SetCommunicationPreferences(true, false);

        Assert.IsTrue(contact.DoNotEmail);
        Assert.IsFalse(contact.DoNotSms);
    }

    [TestMethod]
    public void JournalCreate_AllocatesAggregate()
    {
        var journal = Journal.Create(Guid.NewGuid());

        Assert.IsNotNull(journal);
        Assert.AreNotEqual(Guid.Empty, journal.Id);
        Assert.HasCount(1, journal.DomainEvents);
    }

    [TestMethod]
    public void ReceiptAssignDocument_StoresMetadata()
    {
        var receipt = Receipt.Create(Guid.NewGuid());
        receipt.Id = Guid.NewGuid();

        receipt.AssignDocument(
            $"receipts/{receipt.Id:N}.pdf",
            $"receipt-{receipt.Id:N}.pdf",
            "application/pdf",
            128,
            DateTime.UtcNow);

        Assert.IsTrue(receipt.HasDocument);
        Assert.AreEqual("application/pdf", receipt.DocumentContentType);
        Assert.AreEqual(128, receipt.DocumentSizeBytes);
    }

    [TestMethod]
    public void ContactCreate_WithEmptyCountryId_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Computing Lane",
            "ada@example.com",
            "+61 400 000 000",
            Guid.Empty));
    }

    [TestMethod]
    public void PaymentScheduleCreate_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PaymentSchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0,
            DateOnly.FromDateTime(DateTime.UtcNow),
            RecurringInterval.Monthly));
    }

    [TestMethod]
    public void PaymentScheduleCreate_WhenOneOff_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PaymentSchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            40,
            DateOnly.FromDateTime(DateTime.UtcNow),
            RecurringInterval.OneOff));
    }

    [TestMethod]
    public void TransactionCreate_WithReceivedDateBeforeBookDate_ThrowsArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Transaction.Create(
            25,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 8, 14)));
    }

    [TestMethod]
    public void ReceiptCreate_WithoutTransaction_ReservesNullableLink()
    {
        var receipt = Receipt.Create(Guid.NewGuid());

        Assert.IsNull(receipt.TransactionId);
    }

    [TestMethod]
    public void TransactionCreatePending_RaisesTransactionPending()
    {
        var transaction = Transaction.CreatePending(
            25,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 16),
            isRecurring: false);

        Assert.AreEqual(TransactionStatus.Pending, transaction.Status);
        Assert.IsInstanceOfType(transaction.DomainEvents.Single(), typeof(TransactionPendingDomainEvent));
    }

    [TestMethod]
    public void TransactionMarkSucceeded_RaisesTransactionSucceeded()
    {
        var transaction = Transaction.CreatePending(
            25,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 16),
            isRecurring: false);
        transaction.ClearDomainEvents();

        transaction.MarkSucceeded();

        Assert.AreEqual(TransactionStatus.Succeeded, transaction.Status);
        Assert.IsInstanceOfType(transaction.DomainEvents.Single(), typeof(TransactionSucceededDomainEvent));
    }

    [TestMethod]
    public void TransactionMarkFailed_RaisesTransactionFailed()
    {
        var transaction = Transaction.CreatePending(
            25,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 16),
            isRecurring: false);
        transaction.ClearDomainEvents();

        transaction.MarkFailed();

        Assert.AreEqual(TransactionStatus.Failed, transaction.Status);
        Assert.IsInstanceOfType(transaction.DomainEvents.Single(), typeof(TransactionFailedDomainEvent));
    }

    [TestMethod]
    public void TransactionCreate_DefaultsToSucceededWithoutPendingEvent()
    {
        var transaction = Transaction.Create(
            25,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 16),
            new DateOnly(2026, 8, 16));

        Assert.AreEqual(TransactionStatus.Succeeded, transaction.Status);
        Assert.IsEmpty(transaction.DomainEvents);
    }

    [TestMethod]
    public void PaymentScheduleRaiseDonationCreated_RaisesDonationCreated()
    {
        var schedule = PaymentSchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            40,
            new DateOnly(2026, 8, 16),
            RecurringInterval.Monthly);

        schedule.RaiseDonationCreated();

        var domainEvent = (DonationCreatedDomainEvent)schedule.DomainEvents.Single();
        Assert.IsTrue(domainEvent.IsRecurring);
        Assert.AreEqual(RecurringInterval.Monthly, domainEvent.RecurringInterval);
        Assert.AreEqual(PaymentType.Bank, domainEvent.PaymentType);
    }
}