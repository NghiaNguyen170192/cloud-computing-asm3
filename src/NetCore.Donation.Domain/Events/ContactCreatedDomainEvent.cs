using MediatR;

namespace NetCore.Donation.Domain.Events;

public sealed record ContactCreatedDomainEvent(Guid ContactId, string Email) : INotification;
