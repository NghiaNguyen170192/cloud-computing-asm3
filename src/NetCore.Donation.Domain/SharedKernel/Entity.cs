using MediatR;

namespace NetCore.Donation.Domain.SharedKernel;

public abstract class Entity
{
    private readonly List<INotification> domainEvents = new();

    private int? requestedHashCode;

    public IReadOnlyCollection<INotification> DomainEvents => domainEvents.AsReadOnly();

    public virtual Guid Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime ModifiedDate { get; set; }

    public Guid ModifiedBy { get; set; }

    protected void AddDomainEvent(INotification eventItem)
    {
        domainEvents.Add(eventItem);
    }

    protected void RemoveDomainEvent(INotification eventItem)
    {
        domainEvents?.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        domainEvents?.Clear();
    }

    protected bool IsTransient()
    {
        return Id == default;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || !(obj is Entity))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (GetType() != obj.GetType())
        {
            return false;
        }

        var item = (Entity)obj;

        if (item.IsTransient() || IsTransient())
        {
            return false;
        }

        return item.Id == Id;
    }

    public override int GetHashCode()
    {
        if (!IsTransient())
        {
            if (!requestedHashCode.HasValue)
            {
                requestedHashCode = Id.GetHashCode() ^ 31; // XOR for random distribution (http://blogs.msdn.com/b/ericlippert/archive/2011/02/28/guidelines-and-rules-for-gethashcode.aspx)
            }

            return requestedHashCode.Value;
        }

        return base.GetHashCode();
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return Equals(left, null) ? Equals(right, null) : left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }
}