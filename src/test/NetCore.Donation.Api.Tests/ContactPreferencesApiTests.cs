using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NetCore.Donation.Domain.Entities;
using NetCore.Donation.Infrastructure.Database;

namespace NetCore.Donation.Api.Tests;

[TestClass]
public class ContactPreferencesApiTests
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
    public async Task Contacts_CreateGetAndPatchPreferences_Succeed()
    {
        var countryId = await SeedCountryAsync();

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/contacts",
            new
            {
                firstName = "Ada",
                lastName = "Lovelace",
                dateOfBirth = "1815-12-10",
                addressLine = "1 Analytical Engine Way",
                email = "ada@example.com",
                phoneNumber = "123456",
                countryId,
                doNotEmail = true,
                doNotSms = false,
            });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<IdResponse>();
        Assert.IsNotNull(created);

        var getResponse = await client.GetAsync($"/api/v1/contacts/{created.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        using (var getDocument = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync()))
        {
            Assert.IsTrue(getDocument.RootElement.GetProperty("do-not-email").GetBoolean());
            Assert.IsFalse(getDocument.RootElement.GetProperty("do-not-sms").GetBoolean());
        }

        var patchResponse = await client.PatchAsJsonAsync(
            $"/api/v1/contacts/{created.Id}/preferences",
            new { id = created.Id, doNotEmail = false, doNotSms = true });
        Assert.AreEqual(HttpStatusCode.NoContent, patchResponse.StatusCode);

        var updatedResponse = await client.GetAsync($"/api/v1/contacts/{created.Id}");
        Assert.AreEqual(HttpStatusCode.OK, updatedResponse.StatusCode);
        using (var updatedDocument = JsonDocument.Parse(await updatedResponse.Content.ReadAsStringAsync()))
        {
            Assert.IsFalse(updatedDocument.RootElement.GetProperty("do-not-email").GetBoolean());
            Assert.IsTrue(updatedDocument.RootElement.GetProperty("do-not-sms").GetBoolean());
        }

        var mismatchResponse = await client.PatchAsJsonAsync(
            $"/api/v1/contacts/{created.Id}/preferences",
            new { id = Guid.NewGuid(), doNotEmail = true, doNotSms = true });
        Assert.AreEqual(HttpStatusCode.BadRequest, mismatchResponse.StatusCode);

        var missingId = Guid.NewGuid();
        var missingResponse = await client.PatchAsJsonAsync(
            $"/api/v1/contacts/{missingId}/preferences",
            new { id = missingId, doNotEmail = true, doNotSms = true });
        Assert.AreEqual(HttpStatusCode.NotFound, missingResponse.StatusCode);
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

    private sealed class IdResponse
    {
        public Guid Id { get; set; }
    }
}

