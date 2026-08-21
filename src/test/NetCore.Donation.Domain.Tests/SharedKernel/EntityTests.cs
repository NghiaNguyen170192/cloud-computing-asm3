using NetCore.Donation.Domain.SharedKernel;

namespace NetCore.Donation.Domain.Tests.SharedKernel;

[TestClass]
public class EntityTests
{
    private class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    [TestMethod]
    public void Entity_HasEmptyId_BeforePersistence()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.AreEqual(Guid.Empty, entity.Id);
    }

    [TestMethod]
    public void Entity_HasAuditProperties()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        Assert.AreEqual(default(DateTime), entity.CreatedDate);
        Assert.AreEqual(default(DateTime), entity.ModifiedDate);
        Assert.AreEqual(Guid.Empty, entity.CreatedBy);
        Assert.AreEqual(Guid.Empty, entity.ModifiedBy);
    }

    [TestMethod]
    public void Entity_CanAddDomainEvent()
    {
        // Arrange
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();

        // Act
        entity.GetType()
            .GetMethod("AddDomainEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(entity, new object[] { domainEvent });

        // Assert
        Assert.HasCount(1, entity.DomainEvents);
        Assert.AreSame(domainEvent, entity.DomainEvents.First());
    }

    [TestMethod]
    public void Entity_CanClearDomainEvents()
    {
        // Arrange
        var entity = new TestEntity();
        var addMethod = entity.GetType()
            .GetMethod("AddDomainEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        addMethod.Invoke(entity, new object[] { new TestDomainEvent() });
        addMethod.Invoke(entity, new object[] { new TestDomainEvent() });

        // Act
        entity.ClearDomainEvents();

        // Assert
        Assert.IsEmpty(entity.DomainEvents);
    }

    [TestMethod]
    public void Entity_Equals_ReturnsTrueForSameId()
    {
        // Arrange
        var entity1 = new TestEntity { Name = "Test1" };
        var entity2 = new TestEntity { Name = "Test2" };
        var testId = Guid.NewGuid();
        entity1.Id = testId;
        entity2.Id = testId;

        // Act & Assert
        Assert.IsTrue(entity1.Equals(entity2));
        Assert.IsTrue(entity1 == entity2);
        Assert.IsFalse(entity1 != entity2);
    }

    [TestMethod]
    public void Entity_GetHashCode_ConsistentForSameEntity()
    {
        // Arrange
        var entity = new TestEntity();
        var hashCode1 = entity.GetHashCode();

        // Act
        var hashCode2 = entity.GetHashCode();

        // Assert
        Assert.AreEqual(hashCode1, hashCode2);
    }

    private record TestDomainEvent : MediatR.INotification;
}