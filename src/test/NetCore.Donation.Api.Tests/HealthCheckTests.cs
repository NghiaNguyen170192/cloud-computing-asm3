using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace NetCore.Donation.Api.Tests;

[TestClass]
public class HealthCheckTests
{
    [TestMethod]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // Configure test services if needed
            });

        using var client = application.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task AliveCheck_ReturnsOk()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        // Act
        var response = await client.GetAsync("/alive");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}