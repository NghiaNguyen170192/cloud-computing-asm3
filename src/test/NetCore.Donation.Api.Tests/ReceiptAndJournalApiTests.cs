using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Domain.Enums;
using NetCore.Donation.Infrastructure.Database;

namespace NetCore.Donation.Api.Tests;

[TestClass]
public class ReceiptAndJournalApiTests
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
    public async Task Journals_CreateGetAndDelete_Succeed()
    {
        var transactionId = await SeedTransactionAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/journals", new { transactionId });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.IsNotNull(created);

        var getResponse = await client.GetAsync($"/api/v1/journals/{created.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/v1/journals/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await client.GetAsync($"/api/v1/journals/{created.Id}");
        Assert.AreEqual(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [TestMethod]
    public async Task Receipts_GetById_SupportsJsonAndPdfContentNegotiation()
    {
        var contactId = await SeedContactAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/receipts",
            new { contactId, transactionId = (Guid?)null });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.IsNotNull(created);

        var jsonRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/receipts/{created.Id}");
        jsonRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var jsonResponse = await client.SendAsync(jsonRequest);
        Assert.AreEqual(HttpStatusCode.OK, jsonResponse.StatusCode);
        Assert.AreEqual("application/json", jsonResponse.Content.Headers.ContentType?.MediaType);

        var documentRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/receipts/{created.Id}");
        documentRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        var documentResponse = await client.SendAsync(documentRequest);
        Assert.AreEqual(HttpStatusCode.OK, documentResponse.StatusCode);
        Assert.AreEqual("application/pdf", documentResponse.Content.Headers.ContentType?.MediaType);
        var documentBytes = await documentResponse.Content.ReadAsByteArrayAsync();
        Assert.IsTrue(documentBytes.Length > 0);
        CollectionAssert.AreEqual("%PDF"u8.ToArray(), documentBytes.Take(4).ToArray());

        var unsupportedRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/receipts/{created.Id}");
        unsupportedRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
        var unsupportedResponse = await client.SendAsync(unsupportedRequest);
        Assert.AreEqual(HttpStatusCode.NotAcceptable, unsupportedResponse.StatusCode);
    }

    private async Task<Guid> SeedContactAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var country = Country.Create("Australia", "036", "AU", "AUS");
        await context.Countries.AddAsync(country);
        await context.SaveChangesAsync(CancellationToken.None);

        var contact = Contact.Create(
            "Ada",
            "Lovelace",
            new DateOnly(1815, 12, 10),
            "1 Analytical Engine Way",
            "ada@example.com",
            "123456",
            country.Id);
        await context.Contacts.AddAsync(contact);
        await context.SaveChangesAsync(CancellationToken.None);
        return contact.Id;
    }

    private async Task<Guid> SeedTransactionAsync()
    {
        var contactId = await SeedContactAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
        var contact = await context.Contacts.FindAsync(contactId);
        Assert.IsNotNull(contact);

        var paymentMethod = PaymentMethod.Create(contact.Id, "Visa");
        await context.PaymentMethods.AddAsync(paymentMethod);
        await context.SaveChangesAsync(CancellationToken.None);

        var schedule = PaymentSchedule.Create(
            contact.Id,
            paymentMethod.Id,
            25m,
            new DateOnly(2026, 8, 15),
            RecurringInterval.Monthly);
        await context.PaymentSchedules.AddAsync(schedule);
        await context.SaveChangesAsync(CancellationToken.None);

        var transaction = Transaction.Create(
            25m,
            schedule.Id,
            contact.Id,
            paymentMethod.Id,
            PaymentType.CreditCard,
            new DateOnly(2026, 8, 15),
            new DateOnly(2026, 8, 15));
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync(CancellationToken.None);
        return transaction.Id;
    }

    private sealed class IdResponse
    {
        public Guid Id { get; set; }
    }
}
