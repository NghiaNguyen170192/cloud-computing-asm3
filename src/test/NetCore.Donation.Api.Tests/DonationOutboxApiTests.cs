using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Infrastructure.Database;

namespace NetCore.Donation.Api.Tests;

[TestClass]
public class DonationOutboxApiTests
{
    private ApiWebApplicationFactory factory = null!;
    private HttpClient client = null!;

    [TestInitialize]
    public void Initialize()
    {
        factory = new ApiWebApplicationFactory();
        client = factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        client?.Dispose();
        factory?.Dispose();
    }

    [TestMethod]
    public async Task Donate_DrainsOutbox_CreatesJournalAndReceipt()
    {
        var countryId = await SeedCountryAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/donations",
            new
            {
                firstName = "Nghia",
                lastName = "Nguyen",
                dateOfBirth = "1992-12-01",
                addressLine = "1 Test Street",
                email = "donate-outbox@example.com",
                phoneNumber = "0908170192",
                countryId,
                amount = 50m,
                paymentMethodName = "Visa",
                paymentType = PaymentType.CreditCard,
                isRecurring = false,
                recurringInterval = RecurringInterval.OneOff,
            });

        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<DonateResponse>();
        Assert.IsNotNull(created);
        Assert.IsNotNull(created.TransactionId);

        var transactionResponse = await client.GetAsync($"/api/v1/transactions/{created.TransactionId}");
        Assert.AreEqual(HttpStatusCode.OK, transactionResponse.StatusCode);
        var transaction = await transactionResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.IsNotNull(transaction);
        Assert.AreEqual(TransactionStatus.Succeeded, transaction.Status);
        Assert.IsNotNull(transaction.JournalId);
        Assert.IsNotNull(transaction.ReceiptId);
    }

    [TestMethod]
    public async Task ProcessOutbox_ReturnsProcessedCount()
    {
        var response = await client.PostAsJsonAsync("/api/v1/outbox-messages/process", new { });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProcessResponse>();
        Assert.IsNotNull(body);
        Assert.AreEqual(0, body.Processed);
    }

    private async Task<Guid> SeedCountryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var country = Country.Create("Australia", "036", "AU", "AUS");
        await context.Countries.AddAsync(country);
        await context.SaveChangesAsync(CancellationToken.None);
        return country.Id;
    }

    private sealed class DonateResponse
    {
        public Guid? TransactionId { get; set; }
    }

    private sealed class TransactionResponse
    {
        [JsonPropertyName("status")]
        public TransactionStatus Status { get; set; }

        [JsonPropertyName("journal-id")]
        public Guid? JournalId { get; set; }

        [JsonPropertyName("receipt-id")]
        public Guid? ReceiptId { get; set; }
    }

    private sealed class ProcessResponse
    {
        public int Processed { get; set; }
    }
}
