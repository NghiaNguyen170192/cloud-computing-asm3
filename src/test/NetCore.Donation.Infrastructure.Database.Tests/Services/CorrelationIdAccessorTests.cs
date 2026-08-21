using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NetCore.Donation.Domain.SharedKernel;
using NetCore.Donation.Infrastructure.Database.Services;

namespace NetCore.Donation.Infrastructure.Database.Tests.Services;

[TestClass]
public class CorrelationIdAccessorTests
{
    [TestMethod]
    public void CorrelationId_WhenCorrelationIdInItems_ReturnsCorrelationId()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var items = new Dictionary<object, object?> { { "CorrelationId", "test-correlation-id-123" } };
        httpContextMock.Setup(x => x.Items).Returns(items);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var accessor = new CorrelationIdAccessor(httpContextAccessorMock.Object);

        // Act
        var correlationId = accessor.CorrelationId;

        // Assert
        Assert.AreEqual("test-correlation-id-123", correlationId);
    }

    [TestMethod]
    public void CorrelationId_WhenCorrelationIdNotFound_GeneratesNewGuid()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();
        var items = new Dictionary<object, object?>();
        var headersDictionary = new HeaderDictionary();

        httpContextMock.Setup(x => x.Items).Returns(items);
        httpContextMock.Setup(x => x.Response.Headers).Returns(headersDictionary);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var accessor = new CorrelationIdAccessor(httpContextAccessorMock.Object);

        // Act
        var correlationId = accessor.CorrelationId;

        // Assert
        Assert.IsTrue(!string.IsNullOrEmpty(correlationId));
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(correlationId, @"^[a-f0-9]{32}$"), 
            "Correlation ID should be a GUID in N format");
    }

    [TestMethod]
    public void CorrelationId_WhenHttpContextNull_GeneratesNewGuid()
    {
        // Arrange
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var accessor = new CorrelationIdAccessor(httpContextAccessorMock.Object);

        // Act
        var correlationId = accessor.CorrelationId;

        // Assert
        Assert.IsTrue(!string.IsNullOrEmpty(correlationId));
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(correlationId, @"^[a-f0-9]{32}$"), 
            "Correlation ID should be a GUID in N format");
    }

    [TestMethod]
    public void CorrelationId_ThrowsArgumentNullException_WhenHttpContextAccessorNull()
    {
        // Act & Assert
        try
        {
            var accessor = new CorrelationIdAccessor(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown.");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }
}
