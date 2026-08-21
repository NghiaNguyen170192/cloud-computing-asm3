using MediatR;

namespace NetCore.Donation.Application.Contact.SetActive;

public sealed record SetContactActiveCommand(Guid Id, bool IsActive) : IRequest<bool>;