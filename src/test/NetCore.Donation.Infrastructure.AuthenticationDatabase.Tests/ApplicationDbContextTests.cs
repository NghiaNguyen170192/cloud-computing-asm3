using Microsoft.EntityFrameworkCore;
using NetCore.Donation.Infrastructure.AuthenticationDatabase;

namespace NetCore.Donation.Infrastructure.AuthenticationDatabase.Tests;

[TestClass]
public class ApplicationDbContextTests
{
    [TestMethod]
    public async Task DbContext_CanBeCreated()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert
        Assert.IsNotNull(context);
    }

    [TestMethod]
    public void DbContext_HasIdentityTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        var context = new ApplicationDbContext(options);

        // Assert - Verify DbSets exist (IdentityDbContext provides these)
        Assert.IsNotNull(context.Users);
        Assert.IsNotNull(context.Roles);
        Assert.IsNotNull(context.UserRoles);
    }

    [TestMethod]
    public async Task DbContext_CanSaveAndRetrieveUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new Models.ApplicationUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true
        };

        // Act
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "testuser");

        // Assert
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual("testuser", retrievedUser.UserName);
        Assert.AreEqual("test@example.com", retrievedUser.Email);
    }
}
